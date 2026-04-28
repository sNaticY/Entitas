using Entitas;
using Entitas.CodeGeneration.Attributes;
using Sample.MultiAssembly.Root;

[Shared]
[Event(EventTarget.Any)]
public sealed class ManaComponent : IComponent
{
    [EntityIndex]
    public int Value;
}
