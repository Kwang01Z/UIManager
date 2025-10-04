using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public partial class IFS_Data
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GridLayoutGroup.Axis scrollType;
    [SerializeField] private Vector4D padding;
    [SerializeField] private Vector2 spacing;

    public GridLayoutGroup.Axis ScrollType => scrollType;
    public Vector2 Spacing => spacing;
    public Vector4D Padding => padding;
    protected RectTransform ContentRect => scrollRect.content;
    protected RectTransform ViewportRect => scrollRect.viewport;
    [HideInInspector] public float ViewportWidth;
    [HideInInspector] public float ViewportHeight;
    [HideInInspector] public Vector2 ContentAnchor;
    private IIFS_Visible _scrollVisible;
    private IIFS_Cursor _scrollCursor;
    private void OnValidate()
    {
        if (!Application.isPlaying && scrollRect)
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
        _scrollVisible = IFS_VisibleFactory.Build(scrollType);
        _scrollCursor = IFS_CursorFactory.Build(scrollType);
    }

    protected void Start()
    {
        scrollRect.onValueChanged.AddListener(OnScroll);
    }
    

    private int _frameIgnore = 0;
    public int frameBreak = 2;
    private void OnScroll(Vector2 delta)
    {
        if (_frameIgnore < frameBreak)
        {
            _frameIgnore++;
            Invoke(nameof(InitData), 0.3f);
            return;
        }
        CancelInvoke(nameof(InitData));
        _frameIgnore = 0;
        InitData();
    }

    public Vector2 ContentSize { get; private set; }

    private void UpdateContentSize(Vector2 contentSize)
    {
        ContentSize = contentSize;
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
