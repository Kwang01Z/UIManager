#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;

[CustomPropertyDrawer(typeof(SearchableEnumAttribute))]
public class SearchableEnumDrawer : PropertyDrawer
{
    private bool showSearchField = false;
    private string searchText = "";
    private Vector2 scrollPos;
    private static GUIStyle searchFieldStyle;
    private static GUIStyle resultButtonStyle;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.Enum)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        EditorGUI.BeginProperty(position, label, property);

        Rect dropdownRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
        
        // Label
        EditorGUI.LabelField(new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), label);

        // Display current value as a button to toggle search
        string displayValue = property.enumDisplayNames[property.enumValueIndex];
        if (GUI.Button(dropdownRect, displayValue, EditorStyles.layerMaskField))
        {
            // Chuyển đổi dropdownRect từ local space (của Inspector) sang screen space
            Vector2 screenPoint = GUIUtility.GUIToScreenPoint(new Vector2(dropdownRect.x, dropdownRect.y));
            Rect screenRect = new Rect(screenPoint.x, screenPoint.y, dropdownRect.width, dropdownRect.height);
            SearchableEnumWindow.ShowWindow(property, screenRect);
        }

        EditorGUI.EndProperty();
    }
}

public class SearchableEnumWindow : EditorWindow
{
    private SerializedObject serializedObject;
    private string propertyPath;
    private string[] enumNames;
    private string[] enumDisplayNames;
    private string searchText = "";
    private Vector2 scrollPos;
    private bool _isFirstFrame = true;

    public static void ShowWindow(SerializedProperty property, Rect buttonRectInScreen)
    {
        SearchableEnumWindow window = CreateInstance<SearchableEnumWindow>();
        window.serializedObject = property.serializedObject;
        window.propertyPath = property.propertyPath;
        window.enumNames = property.enumNames;
        window.enumDisplayNames = property.enumDisplayNames;
        float windowWidth = Mathf.Max(buttonRectInScreen.width, 200);
        window.ShowAsDropDown(buttonRectInScreen, new Vector2(windowWidth, 300));
    }

    private void OnGUI()
    {
        if (serializedObject == null)
        {
            Close();
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        GUI.SetNextControlName("SearchField");
        searchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
        
        if (_isFirstFrame)
        {
            GUI.FocusControl("SearchField");
            _isFirstFrame = false;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        for (int i = 0; i < enumDisplayNames.Length; i++)
        {
            if (string.IsNullOrEmpty(searchText) || enumDisplayNames[i].ToLower().Contains(searchText.ToLower()))
            {
                if (GUILayout.Button(enumDisplayNames[i], EditorStyles.label))
                {
                    serializedObject.Update();
                    var property = serializedObject.FindProperty(propertyPath);
                    property.enumValueIndex = i;
                    serializedObject.ApplyModifiedProperties();
                    Close();
                }
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        if (Event.current.type == Event.current.type && Event.current.keyCode == KeyCode.Escape)
        {
            Close();
        }
    }
}
#endif
