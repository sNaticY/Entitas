using Entitas;
using Entitas.CodeGeneration.Attributes;
using Sample.MultiAssembly.Root;

[Shared]
[Cleanup(CleanupMode.RemoveComponent)]
public sealed class ExpiredComponent : IComponent
{
}
