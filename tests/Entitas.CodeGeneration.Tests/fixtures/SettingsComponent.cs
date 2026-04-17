#nullable disable

using Entitas;
using Entitas.CodeGeneration.Attributes;
using MyApp;

namespace MyFeature
{
    [Config]
    [Unique]
    public sealed class SettingsComponent : IComponent
    {
        [PrimaryEntityIndex]
        public string Key;

        [EntityIndex]
        public int Version;
    }
}
