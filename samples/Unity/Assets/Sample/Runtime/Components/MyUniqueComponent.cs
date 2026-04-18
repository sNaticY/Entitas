using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game, Unique]
public sealed class MyUniqueComponent : IComponent
{
    public string Value;
}
