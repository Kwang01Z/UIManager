using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LayerSourcePath
{
    public static string GetPath(string variableName)
    {
        var type = typeof(LayerSourcePath);

        var field = type.GetField(variableName);

        if (field == null)
        {
            throw new ArgumentException($"No field with name {variableName} found in SourcePath");
        }

        return field.GetValue(null) as string;
    }
}

public enum LayerType
{
    
}