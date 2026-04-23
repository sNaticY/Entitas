using System;
using System.Collections.Generic;
using System.Linq;

namespace Entitas
{
    public sealed class ContextSchema
    {
        readonly IComponentHandle[] _componentHandles;
        readonly ContextSystemRegistration[] _cleanupSystems;
        readonly ContextSystemRegistration[] _eventSystems;
        readonly ContextEntityIndexRegistration[] _entityIndices;

        internal ContextSchema(
            string name,
            IComponentHandle[] componentHandles,
            ContextSystemRegistration[] cleanupSystems,
            ContextSystemRegistration[] eventSystems,
            ContextEntityIndexRegistration[] entityIndices)
        {
            Name = name;
            _componentHandles = componentHandles;
            _cleanupSystems = cleanupSystems;
            _eventSystems = eventSystems;
            _entityIndices = entityIndices;
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

        public Systems CreateCleanupSystems(Contexts contexts) =>
            CreateSystems(contexts, _cleanupSystems);

        public Systems CreateEventSystems(Contexts contexts) =>
            CreateSystems(contexts, _eventSystems);

        public void InitializeEntityIndices(Contexts contexts)
        {
            for (var i = 0; i < _entityIndices.Length; i++)
                _entityIndices[i].Initialize(contexts);
        }

        static Systems CreateSystems(Contexts contexts, ContextSystemRegistration[] registrations)
        {
            var systems = new Systems();
            for (var i = 0; i < registrations.Length; i++)
                systems.Add(registrations[i].Factory(contexts));

            return systems;
        }
    }

    public sealed class ContextSchemaBuilder
    {
        readonly string _name;
        readonly List<IComponentHandle> _componentHandles = new List<IComponentHandle>();
        readonly List<ContextSystemRegistration> _cleanupSystems = new List<ContextSystemRegistration>();
        readonly List<ContextSystemRegistration> _eventSystems = new List<ContextSystemRegistration>();
        readonly List<ContextEntityIndexRegistration> _entityIndices = new List<ContextEntityIndexRegistration>();

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

        public ContextSchemaBuilder AddCleanupSystem(string name, Func<Contexts, ISystem> factory) =>
            AddSystem(_cleanupSystems, name, priority: 0, factory);

        public ContextSchemaBuilder AddEventSystem(string name, int priority, Func<Contexts, ISystem> factory) =>
            AddSystem(_eventSystems, name, priority, factory);

        public ContextSchemaBuilder AddEntityIndex(string name, Action<Contexts> initialize)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Entity index registration name must not be empty.", nameof(name));

            if (initialize == null)
                throw new ArgumentNullException(nameof(initialize));

            if (_entityIndices.Any(registration => string.Equals(registration.Name, name, StringComparison.Ordinal)))
            {
                throw new EntitasException(
                    $"Context schema '{_name}' already contains entity index registration '{name}'!",
                    "Each entity index can only be registered once per logical context.");
            }

            _entityIndices.Add(new ContextEntityIndexRegistration(name, initialize));
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

            var cleanupSystems = _cleanupSystems
                .OrderBy(registration => registration.Name, StringComparer.Ordinal)
                .ToArray();

            var eventSystems = _eventSystems
                .OrderBy(registration => registration.Priority)
                .ThenBy(registration => registration.Name, StringComparer.Ordinal)
                .ToArray();

            var entityIndices = _entityIndices
                .OrderBy(registration => registration.Name, StringComparer.Ordinal)
                .ToArray();

            return new ContextSchema(_name, orderedHandles, cleanupSystems, eventSystems, entityIndices);
        }

        ContextSchemaBuilder AddSystem(
            List<ContextSystemRegistration> registrations,
            string name,
            int priority,
            Func<Contexts, ISystem> factory)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Context system registration name must not be empty.", nameof(name));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (registrations.Any(registration => string.Equals(registration.Name, name, StringComparison.Ordinal)))
            {
                throw new EntitasException(
                    $"Context schema '{_name}' already contains system registration '{name}'!",
                    "Each generated system registration can only be added once per logical context.");
            }

            registrations.Add(new ContextSystemRegistration(name, priority, factory));
            return this;
        }
    }

    readonly struct ContextSystemRegistration
    {
        public ContextSystemRegistration(string name, int priority, Func<Contexts, ISystem> factory)
        {
            Name = name;
            Priority = priority;
            Factory = factory;
        }

        public string Name { get; }
        public int Priority { get; }
        public Func<Contexts, ISystem> Factory { get; }
    }

    readonly struct ContextEntityIndexRegistration
    {
        public ContextEntityIndexRegistration(string name, Action<Contexts> initialize)
        {
            Name = name;
            Initialize = initialize;
        }

        public string Name { get; }
        public Action<Contexts> Initialize { get; }
    }
}
