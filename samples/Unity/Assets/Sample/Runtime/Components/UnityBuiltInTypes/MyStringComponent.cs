using Entitas;
using Entitas.CodeGeneration.Attributes;

[Game, Input]
public sealed class MyStringComponent : IComponent
{
    public string Value;

    public override string ToString() => $"MyString({Value})";
}
