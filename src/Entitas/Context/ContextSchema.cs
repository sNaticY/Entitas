using System;
using System.Collections.Generic;
using System.Linq;

namespace Entitas
{
    public sealed class ContextSchema
    {
        readonly IComponentHandle[] _componentHandles;

        internal ContextSchema(string name, IComponentHandle[] componentHandles)
        {
            Name = name;
            _componentHandles = componentHandles;
            ComponentNames = componentHandles.Select(handle => handle.Name).ToArray();
            ComponentTypes = componentHandles.Select(handle => handle.ComponentType).ToArray();
        }

        public string Name { get; }
        public int TotalComponents => _componentHandles.Length;
        public IReadOnlyList<IComponentHandle> ComponentHandles => _componentHandles;
        public string[] ComponentNames { get; }
        public Type[] ComponentTypes { get; }

        public ContextInfo CreateContextInfo(string expectedContextName)
        {
            if (!string.Equals(Name, expectedContextName, StringComparison.Ordinal))
            {
                throw new EntitasException(
                    $"Context schema '{Name}' cannot initialize context '{expectedContextName}'!",
                    "Build the schema with the same logical context name as the generated context type.");
            }

            return new ContextInfo(Name, ComponentNames, ComponentTypes);
        }
    }

    public sealed class ContextSchemaBuilder
    {
        readonly string _name;
        readonly List<IComponentHandle> _componentHandles = new List<IComponentHandle>();

        public ContextSchemaBuilder(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Context schema name must not be empty.", nameof(name));

            _name = name;
        }

        public ContextSchemaBuilder Add(IComponentHandle handle)
        {
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));

            if (_componentHandles.Any(existing => ReferenceEquals(existing, handle)))
                return this;

            var duplicateName = _componentHandles.FirstOrDefault(existing =>
                string.Equals(existing.Name, handle.Name, StringComparison.Ordinal));
            if (duplicateName != null)
            {
                throw new EntitasException(
                    $"Context schema '{_name}' already contains component '{handle.Name}'!",
                    "Each component name can only be registered once per logical context.");
            }

            var duplicateType = _componentHandles.FirstOrDefault(existing => existing.ComponentType == handle.ComponentType);
            if (duplicateType != null)
            {
                throw new EntitasException(
                    $"Context schema '{_name}' already contains component type '{handle.ComponentType}'!",
                    "Each component type can only be registered once per logical context.");
            }

            _componentHandles.Add(handle);
            return this;
        }

        public ContextSchema Build()
        {
            var orderedHandles = _componentHandles
                .OrderBy(handle => handle.ComponentType.Assembly.GetName().Name, StringComparer.Ordinal)
                .ThenBy(handle => handle.ComponentType.FullName, StringComparer.Ordinal)
                .ThenBy(handle => handle.Name, StringComparer.Ordinal)
                .ToArray();

            for (var i = 0; i < orderedHandles.Length; i++)
                orderedHandles[i].AssignIndex(i);

            return new ContextSchema(_name, orderedHandles);
        }
    }
}
