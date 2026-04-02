using LitMotion;
using LitMotion.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TransformStateHandle : AnimationStateBase
{
    [SerializeField] protected Transform transformComponent;
    [SerializeField] protected List<TransformStateData> stateDatas;

    [SerializeField] private int defaultIndex = 0;
    private void Reset()
    {
        if (!transformComponent) transformComponent = GetComponent<Transform>();
        stateDatas = new();
        stateDatas.Add(new()
        {
            Index = 0,
            position = transformComponent.position,
            rotation = transformComponent.rotation,
            localScale = transformComponent.localScale
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
        if (_motionHandle.IsActive())
        {
            _motionHandle.TryCancel();
        }
        var state = stateDatas.Find(x => x.Index == stateIndex);
        if (state == null) return;
        transformComponent.position = state.position;
        transformComponent.rotation = state.rotation;
        transformComponent.localScale = state.localScale;
    }

    private MotionSequenceBuilder _motionSequence = LSequence.Create();
    protected MotionHandle _motionHandle;
    public override void SetupStateSmooth(int stateIndex)
    {
        if (_motionHandle.IsActive())
        {
            _motionHandle.TryCancel();
        }

        var state = stateDatas.Find(x => x.Index == stateIndex);
        if (state == null) return;
        _motionSequence = LSequence.Create();
        _motionSequence.Join(LMotion.Create(transformComponent.position, state.position, duration).WithEase(ease)
            .BindToPosition(transformComponent));
        _motionSequence.Join(LMotion.Create(transformComponent.rotation, state.rotation, duration).WithEase(ease)
            .BindToRotation(transformComponent));
        _motionSequence.Join(LMotion.Create(transformComponent.localScale, state.localScale, duration).WithEase(ease)
            .BindToLocalScale(transformComponent));
        _motionHandle = _motionSequence.Run();
    }

    [Button]
    private void SaveCurrentState()
    {
        if (!transformComponent) transformComponent = GetComponent<Transform>();
        int nextIndex = stateDatas.Count > 0 ? stateDatas[^1].Index + 1 : 0;
        stateDatas.Add(new TransformStateData()
        {
            Index = nextIndex,
            position = transformComponent.position,
            rotation = transformComponent.rotation,
            localScale = transformComponent.localScale
        });
    }

    private void OnDestroy()
    {
        if (_motionHandle.IsActive())
        {
            _motionHandle.TryCancel();
        }
    }
}

[Serializable]
public class TransformStateData
{
    public int Index;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 localScale;
}
