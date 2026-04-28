using Entitas;
using Entitas.CodeGeneration.Attributes;
using Sample.MultiAssembly.Root;

[Shared, Unique]
public sealed class SessionComponent : IComponent
{
    [PrimaryEntityIndex]
    public string Id;
}
