using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

[RequireComponent(typeof(Canvas), typeof(CanvasGroup), typeof(RectTransform))]
public class LayerBase : MonoBehaviour
{
    [SerializeField] protected Canvas canvas;
    [SerializeField] protected CanvasGroup canvasGroup;

    private List<int> _sortOrders = new ();

    protected virtual void OnValidate()
    {
        gameObject.SetActive(false);
        
        #if UNITY_EDITOR
        // Auto-setup Addressable if not already configured
        AutoSetupAddressable();
        #endif
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Automatically setup Addressable entry với key pattern "Layers/{prefabName}"
    /// </summary>
    private void AutoSetupAddressable()
    {
        // Only run in Editor và chỉ cho prefabs
        if (!PrefabUtility.IsPartOfPrefabAsset(this))
            return;
            
        try
        {
            // Get Addressable settings
            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (addressableSettings == null)
            {
                Debug.LogWarning("[LayerBase] Addressable settings not found. Please setup Addressables first.");
                return;
            }
            
            // Get prefab asset path
            string assetPath = AssetDatabase.GetAssetPath(this.gameObject);
            if (string.IsNullOrEmpty(assetPath))
                return;
                
            // Get prefab name without extension
            string prefabName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            string addressableKey = $"Layers/{prefabName}";
            
            // Check if already has addressable entry
            var existingEntry = addressableSettings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
            
            if (existingEntry == null)
            {
                // Create new addressable entry
                var defaultGroup = addressableSettings.DefaultGroup;
                if (defaultGroup == null)
                {
                    Debug.LogWarning($"[LayerBase] No default Addressable group found for {prefabName}");
                    return;
                }
                
                var newEntry = addressableSettings.CreateOrMoveEntry(
                    AssetDatabase.AssetPathToGUID(assetPath), 
                    defaultGroup, 
                    false, 
                    false
                );
                
                if (newEntry != null)
                {
                    newEntry.address = addressableKey;
                    addressableSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, newEntry, true);
                    
                    Debug.Log($"[LayerBase] ✅ Auto-created Addressable entry: '{addressableKey}' for prefab '{prefabName}'");
                }
            }
            else if (existingEntry.address != addressableKey)
            {
                // Update existing entry với correct key pattern nếu khác
                string oldAddress = existingEntry.address;
                existingEntry.address = addressableKey;
                addressableSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, existingEntry, true);
                
                Debug.Log($"[LayerBase] 🔄 Updated Addressable key: '{oldAddress}' → '{addressableKey}' for prefab '{prefabName}'");
            }
            else
            {
                // Already configured correctly
                Debug.Log($"[LayerBase] ✅ Addressable already configured: '{addressableKey}' for prefab '{prefabName}'");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LayerBase] Error setting up Addressable for {gameObject.name}: {e.Message}");
        }
    }
    #endif

    protected virtual void Reset()
    {
        canvas ??= GetComponent<Canvas>();
        canvas.overrideSorting = true;
        
        canvasGroup ??= GetComponent<CanvasGroup>();
        canvasGroup.SetActive(false);
    }
    
    public int GetSortingOrder()
    {
        return _sortOrders.Count > 0 ? _sortOrders[^1] : 0;
    }

    public virtual async UniTask ShowLayerAsync()
    {
        await UniTask.Yield();
        canvasGroup.SetActive(true);
        if(!gameObject.activeInHierarchy) gameObject.SetActive(true);
    }

    public virtual async UniTask HideLayerAsync()
    {
        await UniTask.Yield();
        canvasGroup.SetActive(false);
    }
    public virtual async UniTask CloseLayerAsync(bool force = false)
    {
        await HideLayerAsync();
        if(force) _sortOrders.Clear();
        var order = -10000;
        if (_sortOrders.Count > 1)
        {
            _sortOrders.RemoveAt(_sortOrders.Count - 1);
            order = _sortOrders[^1];
        }
        SetSortOrder(order, false);
    }
    public virtual void SetSortOrder(int order, bool save = true)
    {
        canvas.sortingOrder = order;
        if(save) _sortOrders.Add(order);
    }
}
public class LayerGroup
{
    private Dictionary<LayerType, LayerBase> _layerBases = new ();

    public List<LayerType> LayerTypes => new (_layerBases.Keys);
    public async UniTask CloseGroupAsync()
    {
        var tasks = new List<UniTask>();
        foreach (var layerBase in _layerBases.Values)
        {
            tasks.Add(layerBase.CloseLayerAsync());
        }
        await UniTask.WhenAll(tasks);
    }
    public void AddLayer(LayerType layerType ,LayerBase layerBase)
    {
        _layerBases.Add(layerType, layerBase);
    }
    public bool GetLayerBase(LayerType layerType , out LayerBase layerBase)
    {
        layerBase = _layerBases.GetValueOrDefault(layerType);
        return layerBase != null;
    }

    public void SetSortOrder(int order)
    {
        int subOrder = 1;
        foreach (var layerBase in _layerBases.Values)
        {
            layerBase.SetSortOrder(order + subOrder);
            subOrder++;
        }
    }
    public async UniTask ShowGroupAsync()
    {
        var tasks = new List<UniTask>();
        foreach (var layerBase in _layerBases.Values)
        {
            tasks.Add(layerBase.ShowLayerAsync());
        }
        await UniTask.WhenAll(tasks);
    }
}