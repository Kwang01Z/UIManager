using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LayerSourcePath
{
    public const string Layer01 = "Layers/LayerTest01";
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
    MainMenuLayer = 2,
    GameplayLayer = 3,
    PauseLayer = 4,
    SettingsLayer = 5,
    InventoryLayer = 6,
    ShopLayer = 7,
    LoadingLayer = 8,
    NotificationLayer = 9,
}
