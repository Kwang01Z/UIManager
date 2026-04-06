using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
public class TitleAttribute : PropertyAttribute
{
    public string Title { get; private set; }
    public string Subtitle { get; private set; }
    public bool Bold { get; private set; }
    public bool HorizontalLine { get; private set; }

    public TitleAttribute(string title, string subtitle = null, bool bold = true, bool horizontalLine = true)
    {
        Title = title;
        Subtitle = subtitle;
        Bold = bold;
        HorizontalLine = horizontalLine;
    }
}
