using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HorizontalGradientRenderer : BaseMeshEffect
{
    public Color32 leftColor = Color.black;
    public Color32 rightColor = Color.white;

    public override void ModifyMesh(VertexHelper helper)
    {
        if (!IsActive() || helper.currentVertCount == 0)
            return;

        List<UIVertex> vertices = new List<UIVertex>();
        helper.GetUIVertexStream(vertices);

        float rightX = vertices[0].position.x;
        float leftX = vertices[0].position.x;

        for (int i = 1; i < vertices.Count; i++)
        {
            float temp = vertices[i].position.x;
            if (temp > rightX)
            {
                rightX = temp;
            }
            else if (temp < leftX)
            {
                leftX = temp;
            }
        }

        float uiElementWidth = rightX - leftX;

        UIVertex v = new UIVertex();

        for (int i = 0; i < helper.currentVertCount; i++)
        {
            helper.PopulateUIVertex(ref v, i);
            v.color = Color32.Lerp(leftColor, rightColor, (v.position.x - leftX) / uiElementWidth);
            helper.SetUIVertex(v, i);
        }
    }
}
