using Alchemy.Inspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LayoutElementStateHandle : AnimationStateBase
{
    [SerializeField] protected LayoutElement layoutElement;
    [SerializeField] protected List<LayoutElementStateData> stateDatas;

    [SerializeField] private int defaultIndex = 0;
    private void Reset()
    {
        if (!layoutElement) layoutElement = GetComponent<LayoutElement>();
        stateDatas = new();
        stateDatas.Add(new()
        {
            Index = 0,
            minWidth = layoutElement ? layoutElement.minWidth : 0,
            minHeight = layoutElement ? layoutElement.minHeight : 0,
            preferredWidth = layoutElement ? layoutElement.preferredWidth : -1,
            preferredHeight = layoutElement ? layoutElement.preferredHeight : -1,
            flexibleWidth = layoutElement ? layoutElement.flexibleWidth : 1,
            flexibleHeight = layoutElement ? layoutElement.flexibleHeight : 1,
            layoutPriority = layoutElement ? layoutElement.layoutPriority : 0
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
        var state = stateDatas.Find(x => x.Index == stateIndex);
        if (state == null) return;
        if (!layoutElement) layoutElement = GetComponent<LayoutElement>();
        if (!layoutElement) return;

        layoutElement.minWidth = state.minWidth;
        layoutElement.minHeight = state.minHeight;
        layoutElement.preferredWidth = state.preferredWidth;
        layoutElement.preferredHeight = state.preferredHeight;
        layoutElement.flexibleWidth = state.flexibleWidth;
        layoutElement.flexibleHeight = state.flexibleHeight;
        layoutElement.layoutPriority = state.layoutPriority;
    }

    public override void SetupStateSmooth(int stateIndex)
    {
        var state = stateDatas.Find(x => x.Index == stateIndex);
        if (state == null) return;
        if (!layoutElement) layoutElement = GetComponent<LayoutElement>();
        if (!layoutElement) return;

        // LayoutElement properties are typically set immediately as they affect layout
        // But we can still animate the values if needed for smooth transitions
        SetupState(stateIndex);
    }

    [Button]
    private void SaveCurrentState()
    {
        if (!layoutElement) layoutElement = GetComponent<LayoutElement>();
        if (!layoutElement) return;

        int nextIndex = stateDatas.Count > 0 ? stateDatas[^1].Index + 1 : 0;
        stateDatas.Add(new LayoutElementStateData()
        {
            Index = nextIndex,
            minWidth = layoutElement.minWidth,
            minHeight = layoutElement.minHeight,
            preferredWidth = layoutElement.preferredWidth,
            preferredHeight = layoutElement.preferredHeight,
            flexibleWidth = layoutElement.flexibleWidth,
            flexibleHeight = layoutElement.flexibleHeight,
            layoutPriority = layoutElement.layoutPriority
        });
    }
}

[Serializable]
public class LayoutElementStateData
{
    public int Index;
    public float minWidth;
    public float minHeight;
    public float preferredWidth;
    public float preferredHeight;
    public float flexibleWidth;
    public float flexibleHeight;
    public int layoutPriority;
}
