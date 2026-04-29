using Entitas;
using Entitas.CodeGeneration.Attributes;
using Sample.MultiAssembly.Root;

namespace Sample.MultiAssembly.FeatureA
{
    [Shared, Unique]
    public sealed class PlayerComponent : IComponent
    {
        [PrimaryEntityIndex]
        public string Name;

        public int Level;
    }
}
