using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(TitleAttribute))]
public class TitleDrawer : DecoratorDrawer
{
    public override void OnGUI(Rect position)
    {
        var attr = (TitleAttribute)attribute;
        
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 13;

        position.y += 20;
        if (attr.HorizontalLine)
        {
            Rect lineRect = new Rect(position.x, position.y - 12, position.width, 1);
            EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f));
        }

        EditorGUI.LabelField(new Rect(position.x, position.y - 10, position.width, 20), attr.Title, titleStyle);
        
        if (!string.IsNullOrEmpty(attr.Subtitle))
        {
            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.miniLabel);
            EditorGUI.LabelField(new Rect(position.x, position.y + 10, position.width, 15), attr.Subtitle, subtitleStyle);
        }
    }

    public override float GetHeight()
    {
        var attr = (TitleAttribute)attribute;
        return 30 + (string.IsNullOrEmpty(attr.Subtitle) ? 0 : 15);
    }
}
