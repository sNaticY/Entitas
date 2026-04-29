using Entitas;
using Entitas.CodeGeneration.Attributes;
using Sample.MultiAssembly.Root;

namespace Sample.MultiAssembly.FeatureB
{
    [Shared]
    public sealed class HealthComponent : IComponent
    {
        public int Value;
    }
}
