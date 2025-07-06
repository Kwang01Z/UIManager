using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public partial class InfiniteScrollData
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GridLayoutGroup.Axis scrollType;
    [SerializeField] private Vector4D padding;
    [SerializeField] private Vector2 spacing;
    
    public Vector2 Spacing => spacing;
    public Vector4D Padding => padding;
    public Vector2 ContentSize => scrollRect.content.sizeDelta;
    protected RectTransform ContentRect => scrollRect.content;
    protected RectTransform ViewportRect => scrollRect.viewport;
    public float ViewportWidth { get;private set; }
    public float ViewportHeight { get;private set; }
    
    public Vector2 ContentAnchor {get; private set;}
    private IInfiniteScrollVisible _scrollVisible;
    private IInfiniteScrollCursor _scrollCursor;
    private void OnValidate()
    {
        if (scrollRect)
        {
            scrollRect.horizontal = scrollType == GridLayoutGroup.Axis.Horizontal;
            scrollRect.vertical = scrollType == GridLayoutGroup.Axis.Vertical;

            if (scrollType == GridLayoutGroup.Axis.Vertical)
            {
                ContentRect.anchorMin = new Vector2(0, 1);
                ContentRect.anchorMax = new Vector2(1, 1);
                ContentRect.pivot = new Vector2(0.5f, 1);
                ContentRect.offsetMin = new Vector2(0, 1);
                ContentRect.offsetMax = new Vector2(0, 1);
            }
            else
            {
                ContentRect.anchorMin = new Vector2(0, 0);
                ContentRect.anchorMax = new Vector2(0, 1);
                ContentRect.pivot = new Vector2(0f, 0.5f);
                ContentRect.offsetMin = new Vector2(0, 1);
                ContentRect.offsetMax = new Vector2(0, 1);
            }
            
            ContentRect.anchoredPosition = Vector2.zero;
        }
    }

    protected void Awake()
    {
        _scrollVisible = InfiniteScrollVisibleFactory.Build(scrollType);
        _scrollCursor = InfiniteScrollCursorFactory.Build(scrollType);
    }

    protected void Start()
    {
        scrollRect.onValueChanged.AddListener(OnScroll);
    }

    private void OnScroll(Vector2 arg0)
    {
        InitData();
    }
    
    private void UpdateContentSize(Vector2 contentSize)
    {
        ContentRect.sizeDelta = contentSize;
    }

    public void SetPadding(Vector4D paddingValue)
    {
        padding = paddingValue;
        ReloadData();
    }

    public void SetSpacing(Vector2 spacingValue)
    {
        spacing = spacingValue;
        ReloadData();
    }

    public void SetContentAnchor(Vector2 contentAnchor)
    {
        ContentRect.anchoredPosition = contentAnchor;
    }
}
