using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
public sealed class MyPropertyComponent : IComponent
{
    public string Value { get; set; }
}
