using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
public sealed class MyJaggedArrayComponent : IComponent
{
    public string[][] Value;
}
