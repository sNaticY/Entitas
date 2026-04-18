using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
public sealed class MyCustomObjectComponent : IComponent
{
    public MyCustomObject Value;
}

public class MyCustomObject
{
    public string Name;

    public MyCustomObject(string name)
    {
        Name = name;
    }
}
