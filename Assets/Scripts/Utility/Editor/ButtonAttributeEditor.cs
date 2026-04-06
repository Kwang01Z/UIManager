using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ButtonAttributeUtility
{
    private static readonly Dictionary<Type, List<MethodInfo>> _methodCache = new Dictionary<Type, List<MethodInfo>>();

    public static List<MethodInfo> GetButtonMethods(Type targetType)
    {
        if (_methodCache.TryGetValue(targetType, out var methods))
        {
            return methods;
        }

        methods = new List<MethodInfo>();
        var type = targetType;
        
        while (type != null && type != typeof(MonoBehaviour) && type != typeof(ScriptableObject) && type != typeof(object))
        {
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var currentMethods = type.GetMethods(flags);
            
            foreach (var m in currentMethods)
            {
                try 
                {
                    if (Attribute.IsDefined(m, typeof(ButtonAttribute)))
                    {
                        methods.Add(m);
                    }
                }
                catch 
                {
                    // Ignore reflection errors on specific methods (e.g., generic types issues)
                }
            }
            type = type.BaseType;
        }
        
        _methodCache[targetType] = methods;
        return methods;
    }

    public static void DrawButtons(UnityEngine.Object[] targets)
    {
        if (targets == null || targets.Length == 0) return;

        var targetType = targets[0].GetType();
        var methods = GetButtonMethods(targetType);

        if (methods.Count == 0) return;

        EditorGUILayout.Space();
        foreach (var method in methods)
        {
            ButtonAttribute attr = null;
            GUIColorAttribute guiColor = null;

            try 
            {
                attr = (ButtonAttribute)Attribute.GetCustomAttribute(method, typeof(ButtonAttribute));
                guiColor = (GUIColorAttribute)Attribute.GetCustomAttribute(method, typeof(GUIColorAttribute));
            }
            catch { }
            
            string label = (attr == null || string.IsNullOrEmpty(attr.Name)) ? ObjectNames.NicifyVariableName(method.Name) : attr.Name;
            
            Color oldColor = GUI.backgroundColor;
            if (guiColor != null) GUI.backgroundColor = guiColor.Color;

            float height = GetHeight(attr != null ? attr.Size : ButtonSizes.Medium);
            if (GUILayout.Button(label, GUILayout.Height(height)))
            {
                foreach (var t in targets)
                {
                    method.Invoke(t, null);
                }
            }
            
            if (guiColor != null) GUI.backgroundColor = oldColor;
        }
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

// Ensure it draws for ALL MonoBehaviours naturally at the bottom
[CanEditMultipleObjects]
[CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
public class MonoBehaviourButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ButtonAttributeUtility.DrawButtons(targets);
    }
}

// Also support ScriptableObjects globally
[CanEditMultipleObjects]
[CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
public class ScriptableObjectButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ButtonAttributeUtility.DrawButtons(targets);
    }
}
