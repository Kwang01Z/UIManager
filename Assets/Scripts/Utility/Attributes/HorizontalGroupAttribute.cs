using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public class HorizontalGroupAttribute : Attribute
{
    public string GroupID { get; private set; }
    public float Width { get; private set; }

    public HorizontalGroupAttribute(string groupID, float width = 0)
    {
        GroupID = groupID;
        Width = width;
    }
}
