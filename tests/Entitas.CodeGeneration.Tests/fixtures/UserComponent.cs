#nullable disable

using Entitas;
using Entitas.CodeGeneration.Attributes;
using MyApp;

namespace MyFeature
{
    [Main]
    [Unique]
    [Event(EventTarget.Any, EventType.Added, 1)]
    [Event(EventTarget.Any, EventType.Removed, 2)]
    [Event(EventTarget.Self, EventType.Added, 3)]
    [Event(EventTarget.Self, EventType.Removed, 4)]
    [Cleanup(CleanupMode.RemoveComponent)]
    public sealed class UserComponent : IComponent
    {
        [PrimaryEntityIndex]
        public string Name;

        [EntityIndex]
        public int Age;
    }
}
