using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteScrollPlaceHolder
{
    private InfiniteScrollElement _baseElement;
    
    private Vector2 _anchoredPosition;
    private InfiniteScrollElement _element;
    private object _data;

    private bool _isVisible;

    public InfiniteScrollPlaceHolder(InfiniteScrollElement element, object data)
    {
        _baseElement = element;
        _data = data;
    }

    public void SetVisible(bool visible)
    {
        if(visible ^ _isVisible) return;
        _isVisible = visible;
        if (!visible)
        {
            ReleaseData();
            return;
        }

        _element = PoolHolder.Instance.Get(_baseElement) as InfiniteScrollElement;
        _element?.SetupData(_anchoredPosition, _data);
    }

    public void ReleaseData()
    {
        if (_element)
        {
            PoolHolder.Instance.Release(_element);
            _element = null;
        }
    }
}
