using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
public sealed class MyArrayComponent : IComponent
{
    public string[] Value;
}
