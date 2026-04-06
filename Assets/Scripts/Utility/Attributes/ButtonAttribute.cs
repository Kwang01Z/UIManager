using System;

[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class ButtonAttribute : Attribute
{
    public string Name { get; private set; }
    public ButtonSizes Size { get; private set; }

    public ButtonAttribute() { }
    public ButtonAttribute(string name) { Name = name; }
    public ButtonAttribute(ButtonSizes size) { Size = size; }
    public ButtonAttribute(string name, ButtonSizes size)
    {
        Name = name;
        Size = size;
    }
}

public enum ButtonSizes
{
    Small,
    Medium,
    Large,
    Gigantic
}
