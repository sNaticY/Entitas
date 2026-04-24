using Entitas;
using Entitas.CodeGeneration.Attributes;
using Sample.MultiAssembly.Root;

[assembly: EntitasAssembly("Health")]

namespace Sample.MultiAssembly.FeatureB
{
    [Shared]
    public sealed class HealthComponent : IComponent
    {
        public int Value;
    }
}
