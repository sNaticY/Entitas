using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
public sealed class MyUnsupportedObjectComponent : IComponent
{
    public UnsupportedObject Value;
}

public class UnsupportedObject
{
    public string Value;

    public UnsupportedObject(string value)
    {
        Value = value;
    }
}
