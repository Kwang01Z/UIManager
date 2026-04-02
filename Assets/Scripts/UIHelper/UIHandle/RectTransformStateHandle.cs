using LitMotion;
using LitMotion.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class RectTransformStateHandle : AnimationStateBase
{
    [SerializeField] protected RectTransform rectTransform;
    [SerializeField] protected List<RectTransformStateData> stateDatas;

    [SerializeField] private int defaultIndex = 0;
    private void Reset()
    {
        if (!rectTransform) rectTransform = GetComponent<RectTransform>();
        stateDatas = new();
        stateDatas.Add(new()
        {
            Index = 0,
            localScale = rectTransform.localScale,
            sizeDelta = rectTransform.sizeDelta,
            anchoredPosition = rectTransform.anchoredPosition
        });
    }

    [Button]
    public override void PlayState()
    {
        SetupState(defaultIndex);
    }

    [Button]
    public override void PlayStateSmooth()
    {
        SetupStateSmooth(defaultIndex);
    }

    public override void SetupState(int stateIndex)
    {
        if (_currentMotionHandle.IsActive())
        {
            _currentMotionHandle.TryCancel();
        }
        var state = stateDatas.Find(x => x.Index == stateIndex);
        if (state == null) return;
        rectTransform.localScale = state.localScale;
        rectTransform.sizeDelta = state.sizeDelta;
        rectTransform.anchoredPosition = state.anchoredPosition;
    }

    private MotionSequenceBuilder _motionSequence = LSequence.Create();
    private MotionHandle _currentMotionHandle;
    public override void SetupStateSmooth(int stateIndex)
    {
        if (_currentMotionHandle.IsActive())
        {
            _currentMotionHandle.TryCancel();
        }

        var state = stateDatas.Find(x => x.Index == stateIndex);
        if (state == null) return;
        _motionSequence = LSequence.Create();
        _motionSequence.Join(LMotion.Create(rectTransform.anchoredPosition, state.anchoredPosition, duration).WithEase(ease)
            .BindToAnchoredPosition(rectTransform));
        _motionSequence.Join(LMotion.Create(rectTransform.localScale, state.localScale, duration).WithEase(ease)
            .BindToLocalScale(rectTransform));
        _motionSequence.Join(LMotion.Create(rectTransform.sizeDelta, state.sizeDelta, duration).WithEase(ease)
            .BindToSizeDelta(rectTransform));
        _currentMotionHandle = _motionSequence.Run();
    }

    [Button]
    private void SaveCurrentState()
    {
        if (!rectTransform) rectTransform = GetComponent<RectTransform>();
        int nextIndex = stateDatas.Count > 0 ? stateDatas[^1].Index + 1 : 0;
        stateDatas.Add(new RectTransformStateData()
        {
            Index = nextIndex,
            anchoredPosition = rectTransform.anchoredPosition,
            localScale = rectTransform.localScale,
            sizeDelta = rectTransform.sizeDelta
        });
    }
    // Cleanup khi destroy object
    private void OnDestroy()
    {
        if (_currentMotionHandle.IsActive())
        {
            _currentMotionHandle.TryCancel();
        }
    }
}

[Serializable]
public class RectTransformStateData
{
    public int Index;
    public Vector2 anchoredPosition;
    public Vector3 localScale;
    public Vector2 sizeDelta;
}
