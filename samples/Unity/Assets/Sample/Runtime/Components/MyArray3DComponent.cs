using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
public sealed class MyArray3DComponent : IComponent
{
    public string[,,] Value;
}
