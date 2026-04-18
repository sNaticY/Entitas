using Entitas;
using Entitas.CodeGeneration.Attributes;
using Entitas.Unity;

[Game, DontDrawComponent]
public sealed class MyDontDrawComponent : IComponent
{
    public MySimpleObject Value;
}
