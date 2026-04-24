using Entitas;
using Sample.MultiAssembly.Root;

namespace Sample.MultiAssembly.FeatureA
{
    [Shared]
    public sealed class LevelComponent : IComponent
    {
        public int Value;
    }
}
