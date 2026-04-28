using Entitas;
using Entitas.CodeGeneration.Attributes;
using Sample.MultiAssembly.Root;

[Shared]
[Cleanup(CleanupMode.DestroyEntity)]
public sealed class DestroyedComponent : IComponent
{
}
