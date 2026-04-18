using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game]
public sealed class MyPersonComponent : IComponent
{
    public string Name;
    public string Gender;
}
