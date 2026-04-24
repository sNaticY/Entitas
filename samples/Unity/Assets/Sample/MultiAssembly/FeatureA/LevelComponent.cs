using Entitas;
using Entitas.CodeGeneration.Attributes;
using Sample.MultiAssembly.Root;

namespace Sample.MultiAssembly.FeatureA
{
    [Shared]
    public sealed class LevelComponent : IComponent
    {
        [EntityIndex] public int Value;
    }
}
