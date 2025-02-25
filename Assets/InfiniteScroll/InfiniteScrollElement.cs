using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class InfiniteScrollElement : MonoBehaviour
{
    public IDataLoader DataLoaderPF;
    [SerializeField] IFS_ElementType elementType;
    
    [Tooltip("Số phần tử tối đa trên 1 hàng hoặc 1 cột")]
    [SerializeField] [Min(1)] private int numberFixed = 1;
    
    public int NumberFixed => numberFixed;
    public IFS_ElementType ElementType => elementType;
    public RectTransform RectTransform => transform as RectTransform;

    Vector2 _baseSizeDelta;
    private void Awake()
    {
        ValidateNumberFixed();
        _baseSizeDelta = RectTransform.sizeDelta;
    }

    private void OnValidate()
    {
        ValidateNumberFixed();
    }

    void ValidateNumberFixed()
    {
        bool isStretchWidth = RectTransformUtility.IsStretchWidth(RectTransform);
        bool isStretchHeight = RectTransformUtility.IsStretchHeight(RectTransform);
        if (isStretchWidth || isStretchHeight) numberFixed = 1;
    }

    public void SetupData(Vector2 anchoredPosition, Vector4D margin, object data)
    {
        RectTransform.anchoredPosition = anchoredPosition;
        bool isStretchWidth = RectTransformUtility.IsStretchWidth(RectTransform);
        bool isStretchHeight = RectTransformUtility.IsStretchHeight(RectTransform);
        if (isStretchWidth)
        {
            RectTransform.sizeDelta = new Vector2(
                _baseSizeDelta.x - margin.left - margin.right,
                _baseSizeDelta.y);
        }
        else if (isStretchHeight)
        {
            RectTransform.sizeDelta = new Vector2(
                _baseSizeDelta.x,
                _baseSizeDelta.y - margin.top - margin.bottom);
        }

        if(DataLoaderPF != null && data != null) DataLoaderPF.SetupData(data);
    }
    
}

[CustomEditor(typeof(InfiniteScrollElement))]
public class InfiniteScrollElementEditor : Editor
{
    InfiniteScrollElement _targetObject;
    SerializedProperty _elementType;
    SerializedProperty _numberFixed;
    private void OnEnable()
    {
        _elementType = serializedObject.FindProperty("elementType");
        _numberFixed = serializedObject.FindProperty("numberFixed");
    }

    public override void OnInspectorGUI()
    {
        _targetObject = (InfiniteScrollElement)target;
        EditorUtility.SetDirty(_targetObject);
        
        LoadIDataLoader();
        LoadTypeAndNumberFixed();
        serializedObject.ApplyModifiedProperties();
    }

    private void LoadTypeAndNumberFixed()
    {
        EditorGUILayout.PropertyField(_elementType);
        switch ((IFS_ElementType)_elementType.enumValueIndex)
        {
            case IFS_ElementType.Fixed:
                EditorGUILayout.PropertyField(_numberFixed);
                break;
        }
    }
    
    void LoadIDataLoader()
    {
        _targetObject.DataLoaderPF = (IDataLoader)EditorGUILayout.ObjectField(
            "Data Loader",
            _targetObject.DataLoaderPF as MonoBehaviour,
            typeof(IDataLoader),
            true
        );
        if (_targetObject.DataLoaderPF == null) _targetObject.DataLoaderPF = _targetObject.GetComponent<IDataLoader>();
    }
}
