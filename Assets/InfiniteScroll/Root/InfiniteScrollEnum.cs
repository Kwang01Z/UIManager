using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum IFS_ElementType
{
    Flexible,
    Fixed
}

[Serializable]
public struct Vector4D
{
    public int left;
    public int right;
    public int top;
    public int bottom;
}

