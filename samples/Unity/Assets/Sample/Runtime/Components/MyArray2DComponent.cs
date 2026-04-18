using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
public sealed class MyArray2DComponent : IComponent
{
    public string[,] Value;
}
