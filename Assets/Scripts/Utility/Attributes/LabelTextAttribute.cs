using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public class LabelTextAttribute : PropertyAttribute
{
    public string Text { get; private set; }
    public LabelTextAttribute(string text) { Text = text; }
}
