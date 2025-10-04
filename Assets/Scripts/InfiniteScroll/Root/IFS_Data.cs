using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public partial class IFS_Data : MonoBehaviour
{
    List<IFS_PlaceHolder> _placeHolders = new List<IFS_PlaceHolder>();

    public void InitData(List<IFS_PlaceHolder> placeHolders)
    {
        ClearData();
        AddDataRange(placeHolders);
    }

    public void ClearData()
    {
        foreach (var placeHolder in _placeHolders)
        {
            placeHolder.ReleaseData();
        }
        _placeHolders.Clear();
    }

    public void AddDataRange(List<IFS_PlaceHolder> placeHolders)
    {
        _placeHolders.AddRange(placeHolders);

        if (_placeHolders.Count == 0) return;
        CalculateAnchoredPosition();
        InitData();
    }

    [Button]
    public void ReloadData()
    {
        if (_placeHolders.Count == 0) return;
        CalculateAnchoredPosition();
        InitData();
    }


    private void InitData()
    {
        try
        {
            SetupBaseData();
            UpdateVisible();
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    private void SetupBaseData()
    {
        ContentAnchor = ContentRect.anchoredPosition;
        ViewportWidth = ViewportRect.rect.width;
        ViewportHeight = ViewportRect.rect.height;
    }

    private void CalculateAnchoredPosition()
    {
        SetupBaseData();
        var scrollSize = _scrollCursor.CalculateAnchoredPosition(_placeHolders, this);
        UpdateContentSize(scrollSize);
    }
    private bool _isVisible;
    private void UpdateVisible()
    {
        foreach (var placeHolder in _placeHolders)
        {
            if(placeHolder is IFS_PlaceSpace) continue;
            _isVisible = _scrollVisible.IsVisible(placeHolder, this);
            placeHolder.SetVisible(_isVisible);
            if(placeHolder.IsChangeState) placeHolder.UpdateData(scrollRect.content);
        }
    }
}