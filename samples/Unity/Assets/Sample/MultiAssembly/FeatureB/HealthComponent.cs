using Entitas;
using Entitas.CodeGeneration.Attributes;
using Sample.MultiAssembly.Root;

[assembly: EntitasFeature("Health")]

namespace Sample.MultiAssembly.FeatureB
{
    [Shared]
    public sealed class HealthComponent : IComponent
    {
        public int Value;
    }
}
