using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class GUIColorAttribute : Attribute
{
    public Color Color { get; private set; }
    public GUIColorAttribute(float r, float g, float b, float a = 1.0f) { Color = new Color(r, g, b, a); }
}
