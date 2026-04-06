using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HideLabelAttribute))]
public class HideLabelDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, GUIContent.none, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, GUIContent.none, true);
    }
}
