using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(InfoBoxAttribute))]
public class InfoBoxDrawer : DecoratorDrawer
{
    public override void OnGUI(Rect position)
    {
        var attr = (InfoBoxAttribute)attribute;
        MessageType mt = MessageType.Info;
        switch (attr.Type)
        {
            case InfoMessageType.Warning: mt = MessageType.Warning; break;
            case InfoMessageType.Error: mt = MessageType.Error; break;
            case InfoMessageType.None: mt = MessageType.None; break;
        }
        
        EditorGUI.HelpBox(new Rect(position.x, position.y, position.width, position.height), attr.Text, mt);
    }

    public override float GetHeight()
    {
        var attr = (InfoBoxAttribute)attribute;
        if (attr == null) return 0;

        GUIStyle style = EditorStyles.helpBox;
        // Tránh gọi EditorGUIUtility.currentViewWidth trong GetHeight vì hệ thống UIElements 
        // của Unity có thể gọi nó ngoài context OnGUI, gây lỗi ArgumentException.
        float estimatedWidth = 300f; 
        return Mathf.Max(26f, style.CalcHeight(new GUIContent(attr.Text), estimatedWidth) + 10f);
    }
}
