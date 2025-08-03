using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
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
        Parallel.ForEach(_placeHolders, (placeHolder) => { placeHolder.ReleaseData(); });
        _placeHolders.Clear();
    }

    public void AddDataRange(List<IFS_PlaceHolder> placeHolders)
    {
        _placeHolders.AddRange(placeHolders);

        if (_placeHolders.Count == 0) return;
        CalculateAnchoredPosition();
        InitData();
    }

    public void ReloadData()
    {
        if (_placeHolders.Count == 0) return;
        CalculateAnchoredPosition();
        InitData();
    }


    private void InitData()
    {
        SetupBaseData();
        CalculateVisible();
        UpdateData();
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

    private void CalculateVisible()
    {
        Parallel.ForEach(_placeHolders, (placeHolder) =>
        {
            bool isVisible = _scrollVisible.IsVisible(placeHolder, this);
            placeHolder.SetVisible(isVisible);
        });
    }

    private void UpdateData()
    {
        var placeHolderChange = _placeHolders.FindAll(x => x.IsChangeState);
        foreach (var infiniteScrollPlaceHolder in placeHolderChange)
        {
            infiniteScrollPlaceHolder.UpdateData(scrollRect.content);
        }
    }
}