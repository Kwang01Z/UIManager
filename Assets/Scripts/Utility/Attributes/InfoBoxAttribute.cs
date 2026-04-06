using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
public class InfoBoxAttribute : PropertyAttribute
{
    public string Text { get; private set; }
    public InfoMessageType Type { get; private set; }

    public InfoBoxAttribute(string text, InfoMessageType type = InfoMessageType.Info)
    {
        Text = text;
        Type = type;
    }
}

public enum InfoMessageType
{
    None,
    Info,
    Warning,
    Error
}
