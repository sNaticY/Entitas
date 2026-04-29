using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Entitas.CodeGeneration;

public readonly struct EntitasGeneratorOptions : IEquatable<EntitasGeneratorOptions>
{
    static class AnalyzerConfigKeys
    {
        public const string AssemblyNames = "entitas_generator.assembly_names";

        public static class Component
        {
            public const string CleanupSystems = "entitas_generator.component.cleanup_systems";
            public const string ComponentIndex = "entitas_generator.component.component_index";
            public const string ContextExtension = "entitas_generator.component.context_extension";
            public const string EntityExtension = "entitas_generator.component.entity_extension";
            public const string EntityIndexExtension = "entitas_generator.component.entity_index_extension";
            public const string Events = "entitas_generator.component.events";
            public const string EventSystemsExtension = "entitas_generator.component.event_systems_extension";
            public const string Matcher = "entitas_generator.component.matcher";
        }

        public static class Context
        {
            public const string ComponentIndex = "entitas_generator.context.component_index";
            public const string Generation = "entitas_generator.context.context";
            public const string Entity = "entitas_generator.context.entity";
            public const string Matcher = "entitas_generator.context.matcher";
        }

        public static class VisualDebugging
        {
            public const string Enabled = "entitas_generator.visual_debugging";
            public const string AssemblyNames = "entitas_generator.visual_debugging.assembly_names";
        }
    }

    static readonly ImmutableHashSet<string> DefaultVisualDebuggingAssemblyNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Assembly-CSharp");

    public EntitasGeneratorOptions(
        ImmutableHashSet<string>? assemblyNames,
        bool cleanupGenerationEnabled,
        bool componentComponentIndexGenerationEnabled,
        bool componentContextExtensionGenerationEnabled,
        bool componentEntityExtensionGenerationEnabled,
        bool componentEntityIndexGenerationEnabled,
        bool componentEventsGenerationEnabled,
        bool componentEventSystemsGenerationEnabled,
        bool componentMatcherGenerationEnabled,
        bool contextComponentIndexGenerationEnabled,
        bool contextGenerationEnabled,
        bool contextEntityGenerationEnabled,
        bool contextMatcherGenerationEnabled,
        string assemblyName,
        ImmutableHashSet<string> visualDebuggingAssemblyNames,
        bool visualDebuggingGenerationEnabled)
    {
        AssemblyNames = assemblyNames;
        CleanupGenerationEnabled = cleanupGenerationEnabled;
        ComponentComponentIndexGenerationEnabled = componentComponentIndexGenerationEnabled;
        ComponentContextExtensionGenerationEnabled = componentContextExtensionGenerationEnabled;
        ComponentEntityExtensionGenerationEnabled = componentEntityExtensionGenerationEnabled;
        ComponentEntityIndexGenerationEnabled = componentEntityIndexGenerationEnabled;
        ComponentEventsGenerationEnabled = componentEventsGenerationEnabled;
        ComponentEventSystemsGenerationEnabled = componentEventSystemsGenerationEnabled;
        ComponentMatcherGenerationEnabled = componentMatcherGenerationEnabled;
        ContextComponentIndexGenerationEnabled = contextComponentIndexGenerationEnabled;
        ContextGenerationEnabled = contextGenerationEnabled;
        ContextEntityGenerationEnabled = contextEntityGenerationEnabled;
        ContextMatcherGenerationEnabled = contextMatcherGenerationEnabled;
        AssemblyName = assemblyName;
        VisualDebuggingAssemblyNames = visualDebuggingAssemblyNames;
        VisualDebuggingGenerationEnabled = visualDebuggingGenerationEnabled;
    }

    public ImmutableHashSet<string>? AssemblyNames { get; }
    public bool CleanupGenerationEnabled { get; }
    public bool ComponentComponentIndexGenerationEnabled { get; }
    public bool ComponentContextExtensionGenerationEnabled { get; }
    public bool ComponentEntityExtensionGenerationEnabled { get; }
    public bool ComponentEntityIndexGenerationEnabled { get; }
    public bool ComponentEventsGenerationEnabled { get; }
    public bool ComponentEventSystemsGenerationEnabled { get; }
    public bool ComponentMatcherGenerationEnabled { get; }
    public bool ContextComponentIndexGenerationEnabled { get; }
    public bool ContextGenerationEnabled { get; }
    public bool ContextEntityGenerationEnabled { get; }
    public bool ContextMatcherGenerationEnabled { get; }
    public string AssemblyName { get; }
    public ImmutableHashSet<string> VisualDebuggingAssemblyNames { get; }
    public bool VisualDebuggingGenerationEnabled { get; }

    public bool ComponentsLookupGenerationEnabled =>
        ComponentComponentIndexGenerationEnabled && ContextComponentIndexGenerationEnabled;

    public static EntitasGeneratorOptions From(AnalyzerConfigOptionsProvider optionsProvider, Compilation compilation)
    {
        var syntaxTree = compilation.SyntaxTrees.FirstOrDefault();
        var options = syntaxTree is not null
            ? optionsProvider.GetOptions(syntaxTree)
            : optionsProvider.GlobalOptions;

        return new EntitasGeneratorOptions(
            GetAssemblyNames(options, AnalyzerConfigKeys.AssemblyNames),
            GetBool(options, AnalyzerConfigKeys.Component.CleanupSystems, defaultValue: true),
            GetBool(options, AnalyzerConfigKeys.Component.ComponentIndex, defaultValue: true),
            GetBool(options, AnalyzerConfigKeys.Component.ContextExtension, defaultValue: true),
            GetBool(options, AnalyzerConfigKeys.Component.EntityExtension, defaultValue: true),
            GetBool(options, AnalyzerConfigKeys.Component.EntityIndexExtension, defaultValue: true),
            GetBool(options, AnalyzerConfigKeys.Component.Events, defaultValue: true),
            GetBool(options, AnalyzerConfigKeys.Component.EventSystemsExtension, defaultValue: true),
            GetBool(options, AnalyzerConfigKeys.Component.Matcher, defaultValue: true),
            GetBool(options, AnalyzerConfigKeys.Context.ComponentIndex, defaultValue: true),
            GetBool(options, AnalyzerConfigKeys.Context.Generation, defaultValue: true),
            GetBool(options, AnalyzerConfigKeys.Context.Entity, defaultValue: true),
            GetBool(options, AnalyzerConfigKeys.Context.Matcher, defaultValue: true),
            GetAssemblyName(compilation),
            GetAssemblyNames(options, AnalyzerConfigKeys.VisualDebugging.AssemblyNames, DefaultVisualDebuggingAssemblyNames),
            GetBool(options, AnalyzerConfigKeys.VisualDebugging.Enabled, defaultValue: true));
    }

    public bool ShouldRun(string? assemblyName) =>
        assemblyName is not null
        && (AssemblyNames is null || AssemblyNames.Contains(assemblyName));

    public bool ShouldGenerateVisualDebugging(string? assemblyName) =>
        VisualDebuggingGenerationEnabled
        && assemblyName is not null
        && VisualDebuggingAssemblyNames.Contains(assemblyName);

    public bool Equals(EntitasGeneratorOptions other) =>
        CleanupGenerationEnabled == other.CleanupGenerationEnabled &&
        ComponentComponentIndexGenerationEnabled == other.ComponentComponentIndexGenerationEnabled &&
        ComponentContextExtensionGenerationEnabled == other.ComponentContextExtensionGenerationEnabled &&
        ComponentEntityExtensionGenerationEnabled == other.ComponentEntityExtensionGenerationEnabled &&
        ComponentEntityIndexGenerationEnabled == other.ComponentEntityIndexGenerationEnabled &&
        ComponentEventsGenerationEnabled == other.ComponentEventsGenerationEnabled &&
        ComponentEventSystemsGenerationEnabled == other.ComponentEventSystemsGenerationEnabled &&
        ComponentMatcherGenerationEnabled == other.ComponentMatcherGenerationEnabled &&
        ContextComponentIndexGenerationEnabled == other.ContextComponentIndexGenerationEnabled &&
        ContextGenerationEnabled == other.ContextGenerationEnabled &&
        ContextEntityGenerationEnabled == other.ContextEntityGenerationEnabled &&
        ContextMatcherGenerationEnabled == other.ContextMatcherGenerationEnabled &&
        VisualDebuggingGenerationEnabled == other.VisualDebuggingGenerationEnabled &&
        string.Equals(AssemblyName, other.AssemblyName, StringComparison.Ordinal) &&
        AssemblyNamesEqual(AssemblyNames, other.AssemblyNames) &&
        VisualDebuggingAssemblyNames.SetEquals(other.VisualDebuggingAssemblyNames);

    public override bool Equals(object? obj) => obj is EntitasGeneratorOptions other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (AssemblyName?.GetHashCode() ?? 0);
            hash = hash * 31 + (AssemblyNames?.Count ?? -1);
            hash = hash * 31 + VisualDebuggingAssemblyNames.Count;
            hash = hash * 31 + CleanupGenerationEnabled.GetHashCode();
            hash = hash * 31 + ComponentComponentIndexGenerationEnabled.GetHashCode();
            hash = hash * 31 + ComponentContextExtensionGenerationEnabled.GetHashCode();
            hash = hash * 31 + ComponentEntityExtensionGenerationEnabled.GetHashCode();
            hash = hash * 31 + ComponentEntityIndexGenerationEnabled.GetHashCode();
            hash = hash * 31 + ComponentEventsGenerationEnabled.GetHashCode();
            hash = hash * 31 + ComponentEventSystemsGenerationEnabled.GetHashCode();
            hash = hash * 31 + ComponentMatcherGenerationEnabled.GetHashCode();
            hash = hash * 31 + ContextComponentIndexGenerationEnabled.GetHashCode();
            hash = hash * 31 + ContextGenerationEnabled.GetHashCode();
            hash = hash * 31 + ContextEntityGenerationEnabled.GetHashCode();
            hash = hash * 31 + ContextMatcherGenerationEnabled.GetHashCode();
            hash = hash * 31 + VisualDebuggingGenerationEnabled.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(EntitasGeneratorOptions left, EntitasGeneratorOptions right) => left.Equals(right);
    public static bool operator !=(EntitasGeneratorOptions left, EntitasGeneratorOptions right) => !left.Equals(right);

    static bool AssemblyNamesEqual(ImmutableHashSet<string>? left, ImmutableHashSet<string>? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.SetEquals(right);
    }

    static ImmutableHashSet<string>? GetAssemblyNames(
        AnalyzerConfigOptions options,
        string key)
    {
        if (!options.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            return null;

        var parsed = ParseAssemblyNames(value);

        return parsed.Count == 0 ? null : parsed;
    }

    static ImmutableHashSet<string> GetAssemblyNames(
        AnalyzerConfigOptions options,
        string key,
        ImmutableHashSet<string> defaultValue)
    {
        if (!options.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            return defaultValue;

        var parsed = ParseAssemblyNames(value);

        return parsed.Count == 0 ? defaultValue : parsed;
    }

    static ImmutableHashSet<string> ParseAssemblyNames(string value) => value
        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(entry => entry.Trim())
        .Where(entry => entry.Length > 0)
        .ToImmutableHashSet(StringComparer.Ordinal);

    static bool GetBool(AnalyzerConfigOptions options, string key, bool defaultValue)
    {
        if (!options.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    static string GetAssemblyName(Compilation compilation)
    {
        var configuredName = compilation.Assembly
            .GetAttributes()
            .FirstOrDefault(static attribute =>
                attribute.AttributeClass?.ToDisplayString() == "Entitas.CodeGeneration.Attributes.EntitasAssemblyAttribute")
            ?.ConstructorArguments.FirstOrDefault().Value as string;

        return SanitizeIdentifierName(configuredName)
            ?? SanitizeIdentifierName(compilation.AssemblyName)
            ?? "Assembly";
    }

    static string? SanitizeIdentifierName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var chars = value
            .Where(static ch => char.IsLetterOrDigit(ch) || ch == '_')
            .ToArray();

        if (chars.Length == 0)
            return null;

        var name = new string(chars);
        if (!char.IsLetter(name[0]) && name[0] != '_')
            name = $"_{name}";

        name = char.ToUpperInvariant(name[0]) + name.Substring(1);
        return SyntaxFacts.IsValidIdentifier(name) ? name : null;
    }
}
