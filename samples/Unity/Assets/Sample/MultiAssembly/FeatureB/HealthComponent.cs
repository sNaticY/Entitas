using Entitas;
using Sample.MultiAssembly.Root;

namespace Sample.MultiAssembly.FeatureB
{
    [Shared]
    public sealed class HealthComponent : IComponent
    {
        public int Value;
    }
}
