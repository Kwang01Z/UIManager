using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LabelTextAttribute))]
public class LabelTextDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (LabelTextAttribute)attribute;
        label.text = attr.Text;
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
