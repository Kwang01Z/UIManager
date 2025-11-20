using Alchemy.Inspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageStateHandle : AnimationStateBase
{
    [SerializeField] private Image image;
    [SerializeField] private List<ImageStateData> imageStateDataList;
    [SerializeField] private int defaultIndex = 0;
    private void Reset()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }
        imageStateDataList?.Clear();
        imageStateDataList = new();
        ImageStateData stateData = new ImageStateData
        {
            Index = 0,
            Sprite = image.sprite,
            Color = image.color
        };
        imageStateDataList.Add(stateData);
    }
    [Button]
    public override void PlayState()
    {
        SetupState(defaultIndex);
    }

    public override void SetupState(int stateIndex)
    {
        ImageStateData stateData = imageStateDataList.Find(x => x.Index == stateIndex);
        if (stateData == null) return;
        image.sprite = stateData.Sprite;
        image.color = stateData.Color;
        image.enabled = stateData.Enabled;
    }

    public override void PlayStateSmooth()
    {
        SetupState(defaultIndex);
    }

    public override void SetupStateSmooth(int stateIndex)
    {
        SetupState(stateIndex);
    }
}
[Serializable]
public class ImageStateData
{
    public bool Enabled = true;
    public int Index;
    public Sprite Sprite;
    public Color Color;
}

