using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ButtonAttributeEditor
{
    private static readonly Dictionary<Type, List<MethodInfo>> _methodCache = new Dictionary<Type, List<MethodInfo>>();

    static ButtonAttributeEditor()
    {
        Editor.finishedDefaultHeaderGUI += OnFinishedDefaultHeaderGUI;
    }

    private static void OnFinishedDefaultHeaderGUI(Editor editor)
    {
        if (editor.targets.Length == 0) return;
        
        var targetType = editor.targets[0].GetType();
        if (!_methodCache.TryGetValue(targetType, out var methods))
        {
            methods = new List<MethodInfo>();
            foreach (var m in targetType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (Attribute.IsDefined(m, typeof(ButtonAttribute)))
                {
                    methods.Add(m);
                }
            }
            _methodCache[targetType] = methods;
        }

        if (methods.Count == 0) return;

        EditorGUILayout.Space();
        foreach (var method in methods)
        {
            var attr = (ButtonAttribute)Attribute.GetCustomAttribute(method, typeof(ButtonAttribute));
            var guiColor = (GUIColorAttribute)Attribute.GetCustomAttribute(method, typeof(GUIColorAttribute));
            
            string label = (attr == null || string.IsNullOrEmpty(attr.Name)) ? ObjectNames.NicifyVariableName(method.Name) : attr.Name;
            
            Color oldColor = GUI.backgroundColor;
            if (guiColor != null) GUI.backgroundColor = guiColor.Color;

            float height = GetHeight(attr != null ? attr.Size : ButtonSizes.Medium);
            if (GUILayout.Button(label, GUILayout.Height(height)))
            {
                foreach (var t in editor.targets)
                {
                    method.Invoke(t, null);
                }
            }
            
            if (guiColor != null) GUI.backgroundColor = oldColor;
        }
        EditorGUILayout.Space();
    }

    private static float GetHeight(ButtonSizes size)
    {
        switch (size)
        {
            case ButtonSizes.Small: return 18;
            case ButtonSizes.Medium: return 24;
            case ButtonSizes.Large: return 32;
            case ButtonSizes.Gigantic: return 48;
            default: return 22;
        }
    }
}
