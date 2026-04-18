using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
public sealed class MyEnumComponent : IComponent
{
    public MyEnum Value;
}

public enum MyEnum
{
    Item1,
    Item2,
    Item3
}
