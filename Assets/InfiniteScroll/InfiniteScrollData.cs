using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public partial class InfiniteScrollData : MonoBehaviour
{
    List<InfiniteScrollPlaceHolder> _placeHolders = new List<InfiniteScrollPlaceHolder>();

    public void InitData(List<InfiniteScrollPlaceHolder> placeHolders)
    {
        ClearData();
        AddDataRange(placeHolders);
    }
    
    public void ClearData()
    {
        Parallel.ForEach(_placeHolders, (placeHolder) =>
        {
            placeHolder.ReleaseData();
        });
        _placeHolders.Clear();
    }

    public void AddDataRange(List<InfiniteScrollPlaceHolder> placeHolders)
    {
        _placeHolders.AddRange(placeHolders);
        
        if(_placeHolders.Count == 0) return;
        CalculateAnchoredPosition();
        UpdateData();
    }

    private void CalculateAnchoredPosition()
    {
        foreach (var placeHolder in _placeHolders)
        {
            
        }
    }

    void UpdateData()
    {
        Parallel.ForEach(_placeHolders, (placeHolder) =>
        {
            bool isVisible = IsVisible(placeHolder);
            placeHolder.SetVisible(isVisible);
        });
    }

    bool IsVisible(InfiniteScrollPlaceHolder placeHolder)
    {
        return false;
    }
}
