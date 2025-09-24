using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LayerSourcePath
{
    public const string Layer01 = "Layers/LayerTest01";
    public const string Layer8 = "Layers/Layer8";
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
    Layer01 = 1,
    Layer02 = 2,
    Layer03 = 3,
    Layer04 = 4,
    Layer05 = 5,
    Layer06 = 6,
    Layer8 = 7,
}
