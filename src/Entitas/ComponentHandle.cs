using System;

namespace Entitas
{
    public interface IComponentHandle
    {
        string Name { get; }
        Type ComponentType { get; }
        int Index { get; }
        bool IsAssigned { get; }
        void AssignIndex(int index);
    }

    public sealed class ComponentHandle<TComponent> : IComponentHandle where TComponent : IComponent
    {
        int _index = -1;

        public ComponentHandle(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Component handle name must not be empty.", nameof(name));

            Name = name;
        }

        public string Name { get; }
        public Type ComponentType => typeof(TComponent);
        public bool IsAssigned => _index >= 0;

        public int Index
        {
            get
            {
                if (!IsAssigned)
                {
                    throw new EntitasException(
                        $"Component handle '{Name}' for component type '{ComponentType.FullName}' has not been assigned to a context slot!",
                        "Add the component or feature to the ContextSchemaBuilder, build the schema, and create the context with that schema before using generated component APIs.");
                }

                return _index;
            }
        }

        public void AssignIndex(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Component handle index must be zero or greater.");

            if (IsAssigned && _index != index)
            {
                throw new EntitasException(
                    $"Component handle '{Name}' is already assigned to index {_index} and cannot be reassigned to {index}!",
                    "A component handle can only belong to one context schema slot.");
            }

            _index = index;
        }
    }
}
