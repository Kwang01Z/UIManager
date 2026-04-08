using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AutoGetAttribute))]
public class AutoGetDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Kiểm tra xem field có phải là kiểu object reference và đang bị null hay không
        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
        {
            var targetObj = property.serializedObject.targetObject as Component;
            if (targetObj != null)
            {
                // Cố gắng lấy component tương ứng với field type (Hỗ trợ cả interface và class)
                var component = targetObj.GetComponent(fieldInfo.FieldType);
                if (component != null)
                {
                    property.objectReferenceValue = component;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        // Vẽ field hiển thị trong Inspector như mặc định
        EditorGUI.PropertyField(position, property, label);
    }
}
