using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class DictionaryDrawerSettingsAttribute : Attribute
{
    public string KeyLabel { get; set; }
    public string ValueLabel { get; set; }
    public bool DisplayMode { get; set; }
}
