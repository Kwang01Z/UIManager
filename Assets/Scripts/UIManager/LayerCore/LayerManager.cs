using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Profiling;
using UnityEngine;

public partial class LayerManager : MonoSingleton<LayerManager>
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private bool hasLayerRoot = true;
    [SerializeField] private RectTransform layerParent;
    [SerializeField] private LayerReferenceSO layerReferenceSO;
    [SerializeField] private List<LayerReferenceData> layerPreload;
    public static int LimitLayer = 64;
    public int spaceBetweenLayer = 100;
    private static readonly ProfilerMarker ReconcileMarker = new("LayerManager.Reconcile");
    private readonly Dictionary<LayerType, LayerBase> _createdLayerBases = new(LimitLayer);
    private readonly HashSet<LayerType> _showingLayerTypes = new(LimitLayer);
    // Bottom -> top. Indexed traversal and in-place compaction need no temporary stack.
    private readonly List<ShowLayerGroupData> _showingLayerGroups = new(LimitLayer);
    private readonly List<ShowLayerGroupData> _fixedGroups = new(4);
    private readonly List<ShowLayerGroupData> _transientGroups = new(4);
    private readonly Queue<LayerCommand> _commands = new(8);
    private readonly Dictionary<LayerType, LayerState> _layerStates = new(LimitLayer);
    private List<LayerState> _ownedLayers = new(LimitLayer);
    private List<LayerState> _nextOwnedLayers = new(LimitLayer);
    private CancellationToken _destroyToken;
    private bool _layersEnabled = true;
    private uint _reconcileVersion;
    private bool _initializedLayerState;
    public bool IsShowing { get; private set; }
    public static ActionSealed<ShowLayerGroupData> OnCloseLayerGroup = new();

    private sealed class LayerState
    {
        public LayerType Type;
        public LayerBase Layer;
        public readonly List<int> Orders = new(4);
        public uint Version;
        public int VisibilityEpoch;
    }

    private struct LayerCommand
    {
        public ShowLayerGroupData ShowData;
        public bool CloseAll;
    }

    private void Reset()
    {
        if (!layerParent) layerParent = transform as RectTransform;
    }

    public void SetActiveLayerAll(bool active)
    {
        _layersEnabled = active;
        if (canvasGroup)
        {
            if (canvasGroup.alpha != (active ? 1f : 0f)) canvasGroup.alpha = active ? 1f : 0f;
            if (canvasGroup.blocksRaycasts != active) canvasGroup.blocksRaycasts = active;
            if (canvasGroup.interactable != active) canvasGroup.interactable = active;
        }
        foreach (var layer in _createdLayerBases.Values)
        {
            if (layer) layer.SetRenderingEnabled(active);
        }
    }

    protected override void Awake()
    {
        base.Awake();
        _destroyToken = this.GetCancellationTokenOnDestroy();
        if (layerPreload == null) return;
        for (int i = 0; i < layerPreload.Count; i++)
        {
            var data = layerPreload[i];
            if (data != null && data.layerBase && _createdLayerBases.TryAdd(data.layerType, data.layerBase))
            {
                data.layerBase.SetRenderingEnabled(_layersEnabled);
                GetLayerState(data.layerType, data.layerBase);
            }
        }
    }

    // Public snapshots escape to close observers/debuggers: never pool or reuse them.
    public void ShowGroupLayerAsync(ShowLayerGroupData showData)
    {
        if (showData == null) throw new ArgumentNullException(nameof(showData));
        Submit(new LayerCommand { ShowData = showData.Snapshot() });
    }

    public void CloseLastLayerGroup() => Submit(new LayerCommand());
    public void CloseAllLayerGroups() => Submit(new LayerCommand { CloseAll = true });

    private void Submit(LayerCommand command)
    {
        if (!PlayerLoopHelper.IsMainThread)
        {
            SubmitOnMainThreadAsync(command).Forget();
            return;
        }
        Enqueue(command);
    }

    private async UniTask SubmitOnMainThreadAsync(LayerCommand command)
    {
        await UniTask.SwitchToMainThread();
        Enqueue(command);
    }

    private void Enqueue(LayerCommand command)
    {
        if (!this || _destroyToken.IsCancellationRequested) return;
        _commands.Enqueue(command);
        if (IsShowing) return;
        IsShowing = true;
        ProcessCommandsAsync().Forget();
    }

    private async UniTask ProcessCommandsAsync()
    {
        try
        {
            while (_commands.Count > 0 && !_destroyToken.IsCancellationRequested)
            {
                var next = _commands.Dequeue();
                try
                {
                    if (next.ShowData != null)
                        await InternalProcessShowGroupAsync(next.ShowData);
                    else if (next.CloseAll)
                    {
                        int keep = hasLayerRoot ? 1 : 0;
                        while (_showingLayerGroups.Count > keep)
                            InternalCloseLastLayerGroup();
                    }
                    else InternalCloseLastLayerGroup();
                }
                catch (OperationCanceledException) when (_destroyToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }
        }
        finally
        {
            if (_destroyToken.IsCancellationRequested) _commands.Clear();
            IsShowing = false;
        }
    }

    private async UniTask InternalProcessShowGroupAsync(ShowLayerGroupData showData)
    {
        if (showData.LayerTypes.Count == 0)
            throw new ArgumentException("A layer group must contain at least one layer.");
        // Only callbacks expose LayerGroup. Without callbacks we can use the request directly.
        LayerGroup result = showData.OnInitData != null || showData.OnShowComplete != null ? new LayerGroup() : null;
        for (int i = 0; i < showData.LayerTypes.Count; i++)
        {
            var type = showData.LayerTypes[i];
            var layer = InitLayerBase(type);
            if (!layer) throw new InvalidOperationException($"Layer {type} has no prefab/reference.");
            result?.AddLayer(type, layer);
        }
        await UniTask.NextFrame(cancellationToken: _destroyToken);
        showData.OnInitData?.Invoke(result);
        await UniTask.NextFrame(cancellationToken: _destroyToken);

        _transientGroups.Clear();
        if (showData.CloseAllOtherLayer) _showingLayerGroups.Clear();
        else
        {
            for (int i = _showingLayerGroups.Count - 1; i >= 0; i--)
            {
                if (!SameGroup(_showingLayerGroups[i], showData)) continue;
                _showingLayerGroups.RemoveRange(i, _showingLayerGroups.Count - i);
                break;
            }
        }
        CompactGroups(_showingLayerGroups, showData, showData.CloseAllPopup);
        CompactGroups(_fixedGroups, showData, false);
        if (showData.AddToStack) _showingLayerGroups.Add(showData);
        else if (showData.FixedLayer) _fixedGroups.Add(showData);
        else _transientGroups.Add(showData);

        ReconcileLayers(showData);
        if (showData.DisplayImmediately)
        {
            if (result != null) result.ShowGroupAsync();
            else
            {
                for (int i = 0; i < showData.LayerTypes.Count; i++)
                    _createdLayerBases[showData.LayerTypes[i]].ShowLayerAsync();
            }
        }
        await UniTask.NextFrame(cancellationToken: _destroyToken);
        showData.OnShowComplete?.Invoke(result);
    }

    private static bool SameGroup(ShowLayerGroupData left, ShowLayerGroupData right)
    {
        if (left.ID != 0 && left.ID == right.ID) return true;
        if (left.LayerGroupType != right.LayerGroupType || left.LayerTypes.Count != right.LayerTypes.Count) return false;
        for (int i = 0; i < left.LayerTypes.Count; i++)
        {
            if (left.LayerTypes[i] != right.LayerTypes[i]) return false;
        }
        return true;
    }

    private static void CompactGroups(List<ShowLayerGroupData> groups, ShowLayerGroupData opening, bool closePopups)
    {
        int retained = 0;
        for (int i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            if ((closePopups && group.LayerGroupType == LayerGroupType.Popup) || SameGroup(group, opening)) continue;
            groups[retained++] = group;
        }
        if (retained < groups.Count) groups.RemoveRange(retained, groups.Count - retained);
    }

    private void InternalCloseLastLayerGroup()
    {
        int lastIndex = _showingLayerGroups.Count - 1;
        if (lastIndex < (hasLayerRoot ? 1 : 0)) return;
        var last = _showingLayerGroups[lastIndex];
        _showingLayerGroups.RemoveAt(lastIndex);
        ReconcileLayers();
        OnCloseLayerGroup?.Invoke(last);
    }

    private LayerState GetLayerState(LayerType type, LayerBase layer)
    {
        if (_layerStates.TryGetValue(type, out var state))
        {
            state.Layer = layer;
            return state;
        }
        state = new LayerState { Type = type, Layer = layer };
        _layerStates.Add(type, state);
        return state;
    }

    // Reuse per-layer order buffers. Visit current/previous owners, never the entire prefab cache.
    // An epoch replaces clearing a visibility set for each fullscreen group.
    private void ReconcileLayers(ShowLayerGroupData opening = null)
    {
        using var sample = ReconcileMarker.Auto();
        // Scene preloads may start visible or with a saved order. Adopt them once,
        // then only current/previous owners participate in subsequent transitions.
        if (!_initializedLayerState)
        {
            _initializedLayerState = true;
            foreach (var pair in _createdLayerBases)
            {
                if (pair.Value && (pair.Value.IsActive() || pair.Value.GetSortingOrder() != 0))
                    _ownedLayers.Add(GetLayerState(pair.Key, pair.Value));
            }
        }
        unchecked { _reconcileVersion++; }
        if (_reconcileVersion == 0)
        {
            foreach (var state in _layerStates.Values) state.Version = uint.MaxValue;
        }
        _nextOwnedLayers.Clear();
        int order = 0, epoch = 0;
        int spacing = Mathf.Max(1, spaceBetweenLayer);
        AccumulateGroups(_showingLayerGroups, spacing, ref order, ref epoch);
        AccumulateGroups(_transientGroups, spacing, ref order, ref epoch);
        AccumulateGroups(_fixedGroups, spacing, ref order, ref epoch);

        for (int i = 0; i < _ownedLayers.Count; i++)
        {
            var state = _ownedLayers[i];
            if (state.Version != _reconcileVersion && state.Layer)
                state.Layer.CloseLayerAsync(true);
        }
        _showingLayerTypes.Clear();
        for (int i = 0; i < _nextOwnedLayers.Count; i++)
        {
            var state = _nextOwnedLayers[i];
            var layer = state.Layer;
            if (!layer) continue;
            layer.SynchronizeSortOrders(state.Orders);
            if (state.VisibilityEpoch == epoch)
            {
                _showingLayerTypes.Add(state.Type);
                if (opening == null || !opening.LayerTypes.Contains(state.Type))
                    layer.ShowLayerWithoutEvent();
            }
            else if (layer.IsActive()) layer.HideLayerAsync();
        }
        var previous = _ownedLayers;
        _ownedLayers = _nextOwnedLayers;
        _nextOwnedLayers = previous;
    }

    private void AccumulateGroups(List<ShowLayerGroupData> groups, int spacing, ref int order, ref int epoch)
    {
        for (int i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            order += spacing;
            if (!group.FixedLayer && group.HideAllOtherLayer) epoch++;
            for (int j = 0; j < group.LayerTypes.Count; j++)
            {
                var type = group.LayerTypes[j];
                var layer = _createdLayerBases[type];
                var state = GetLayerState(type, layer);
                if (state.Version != _reconcileVersion)
                {
                    state.Version = _reconcileVersion;
                    state.Orders.Clear();
                    _nextOwnedLayers.Add(state);
                }
                state.Orders.Add(order + j + 1);
                state.VisibilityEpoch = epoch;
            }
            order += group.LayerTypes.Count / spacing * spacing;
        }
    }

    private LayerBase InitLayerBase(LayerType type)
    {
        if (_createdLayerBases.TryGetValue(type, out var existing) && existing) return existing;
        var prefab = layerReferenceSO ? layerReferenceSO.GetLayerBase(type) : null;
        if (!prefab) return null;
        var layer = Instantiate(prefab, layerParent);
        layer.SetRenderingEnabled(_layersEnabled);
        layer.SetVisible(false);
        _createdLayerBases[type] = layer;
        GetLayerState(type, layer);
        return layer;
    }

    public HashSet<LayerType> GetShowingLayerTypes() => new(_showingLayerTypes);
    public bool IsLayerShowing(LayerType type) => _showingLayerTypes.Contains(type);

    // Caller-owned buffers avoid allocations for gameplay code polling UI state.
    public void CopyShowingLayerTypesTo(List<LayerType> destination)
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        destination.Clear();
        foreach (var type in _showingLayerTypes) destination.Add(type);
    }

    public HashSet<LayerType> GetLastGroupShowingLayerTypes()
    {
        return _showingLayerGroups.Count == 0 ? new HashSet<LayerType>() :
            new HashSet<LayerType>(_showingLayerGroups[_showingLayerGroups.Count - 1].LayerTypes);
    }

#if UNITY_EDITOR
    public Stack<ShowLayerGroupData> GetShowingLayerGroups()
    {
        var copy = new Stack<ShowLayerGroupData>(_showingLayerGroups.Count);
        for (int i = 0; i < _showingLayerGroups.Count; i++) copy.Push(_showingLayerGroups[i].Snapshot());
        return copy;
    }

    public Dictionary<LayerType, LayerBase> GetCreatedLayerBases() => new(_createdLayerBases);
#endif
}
public static class LayerGroupBuilder
{
    private static int _idCounter;
    public static ShowLayerGroupData Build(LayerGroupType groupType, LayerType layerTypes)
    {
        var data = new ShowLayerGroupData
        {
            ID = Interlocked.Increment(ref _idCounter),
            LayerGroupType = groupType
        };
        data.LayerTypes.Add(layerTypes);
        data.ValidateData();
        return data;
    }

    [Obsolete]
    [Tooltip("Hàm này có thể gây hiệu năng không mong muốn")]
    public static ShowLayerGroupData Build(LayerGroupType groupType, List<LayerType> layerTypes)
    {
        var data = new ShowLayerGroupData
        {
            ID = Interlocked.Increment(ref _idCounter),
            LayerGroupType = groupType
        };
        data.LayerTypes.AddRange(layerTypes);
        data.ValidateData();
        return data;
    }
}

public class ShowLayerGroupData
{
    public int ID;
    public readonly List<LayerType> LayerTypes;

    public ShowLayerGroupData() : this(3) { }
    private ShowLayerGroupData(int capacity) => LayerTypes = new List<LayerType>(capacity);
    public LayerGroupType LayerGroupType;

    public bool CloseAllOtherLayer;
    public bool HideAllOtherLayer;
    public bool CloseAllPopup;
    public bool CloseOtherLayerOver = true;

    public bool AddToStack = true;
    public bool FixedLayer = false;

    public Action<LayerGroup> OnInitData;
    public Action<LayerGroup> OnShowComplete;
    public readonly bool DisplayImmediately = true;
    public ShowLayerGroupData Snapshot()
    {
        var copy = new ShowLayerGroupData(LayerTypes.Count)
        {
            ID = ID,
            LayerGroupType = LayerGroupType,
            CloseAllOtherLayer = CloseAllOtherLayer,
            HideAllOtherLayer = HideAllOtherLayer,
            CloseAllPopup = CloseAllPopup,
            CloseOtherLayerOver = CloseOtherLayerOver,
            AddToStack = AddToStack,
            FixedLayer = FixedLayer,
            OnInitData = OnInitData,
            OnShowComplete = OnShowComplete
        };
        foreach (var type in LayerTypes)
        {
            if (!copy.LayerTypes.Contains(type)) copy.LayerTypes.Add(type);
        }
        return copy;
    }

    public void ValidateData()
    {
        if (LayerGroupType == LayerGroupType.Custom) return;
        CloseAllOtherLayer = LayerGroupType == LayerGroupType.Root;
        CloseOtherLayerOver = LayerGroupType == LayerGroupType.FullScreen;
        HideAllOtherLayer = LayerGroupType == LayerGroupType.FullScreen;
        CloseAllPopup = LayerGroupType == LayerGroupType.FullScreen;
        AddToStack = LayerGroupType != LayerGroupType.Fixed && LayerGroupType != LayerGroupType.Notify;
        FixedLayer = LayerGroupType == LayerGroupType.Fixed;
    }
    public ShowLayerGroupData AddLayer(LayerType layerType)
    {
        LayerTypes.Add(layerType);
        return this;
    }
}

public enum LayerGroupType
{
    None = -1,
    Custom = 0,
    Root = 1,
    FullScreen = 2,
    Popup = 3,
    Notify = 4,
    Fixed = 5
}
