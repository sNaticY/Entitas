using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Entitas.CodeGeneration;

public readonly struct EntitasGeneratorOptions
{
    const string AssemblyNamesKey = "entitas_generator.assembly_names";
    const string ComponentCleanupSystemsKey = "entitas_generator.component.cleanup_systems";
    const string ComponentComponentIndexKey = "entitas_generator.component.component_index";
    const string ComponentContextExtensionKey = "entitas_generator.component.context_extension";
    const string ComponentEntityExtensionKey = "entitas_generator.component.entity_extension";
    const string ComponentEntityIndexExtensionKey = "entitas_generator.component.entity_index_extension";
    const string ComponentEventsKey = "entitas_generator.component.events";
    const string ComponentEventSystemsExtensionKey = "entitas_generator.component.event_systems_extension";
    const string ComponentMatcherKey = "entitas_generator.component.matcher";
    const string ContextComponentIndexKey = "entitas_generator.context.component_index";
    const string ContextContextKey = "entitas_generator.context.context";
    const string ContextEntityKey = "entitas_generator.context.entity";
    const string ContextMatcherKey = "entitas_generator.context.matcher";
    const string VisualDebuggingKey = "entitas_generator.visual_debugging";
    const string VisualDebuggingAssemblyNamesKey = "entitas_generator.visual_debugging.assembly_names";

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
        string featureName,
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
        FeatureName = featureName;
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
    public string FeatureName { get; }
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
            GetAssemblyNames(options, AssemblyNamesKey),
            GetBool(options, ComponentCleanupSystemsKey, defaultValue: true),
            GetBool(options, ComponentComponentIndexKey, defaultValue: true),
            GetBool(options, ComponentContextExtensionKey, defaultValue: true),
            GetBool(options, ComponentEntityExtensionKey, defaultValue: true),
            GetBool(options, ComponentEntityIndexExtensionKey, defaultValue: true),
            GetBool(options, ComponentEventsKey, defaultValue: true),
            GetBool(options, ComponentEventSystemsExtensionKey, defaultValue: true),
            GetBool(options, ComponentMatcherKey, defaultValue: true),
            GetBool(options, ContextComponentIndexKey, defaultValue: true),
            GetBool(options, ContextContextKey, defaultValue: true),
            GetBool(options, ContextEntityKey, defaultValue: true),
            GetBool(options, ContextMatcherKey, defaultValue: true),
            GetFeatureName(compilation),
            GetAssemblyNames(options, VisualDebuggingAssemblyNamesKey, DefaultVisualDebuggingAssemblyNames),
            GetBool(options, VisualDebuggingKey, defaultValue: true));
    }

    public bool ShouldRun(string? assemblyName) =>
        assemblyName is not null
        && (AssemblyNames is null || AssemblyNames.Contains(assemblyName));

    public bool ShouldGenerateVisualDebugging(string? assemblyName) =>
        VisualDebuggingGenerationEnabled
        && assemblyName is not null
        && VisualDebuggingAssemblyNames.Contains(assemblyName);

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

    static string GetFeatureName(Compilation compilation)
    {
        var configuredName = compilation.Assembly
            .GetAttributes()
            .FirstOrDefault(static attribute =>
                attribute.AttributeClass?.ToDisplayString() == "Entitas.CodeGeneration.Attributes.EntitasFeatureAttribute")
            ?.ConstructorArguments.FirstOrDefault().Value as string;

        return SanitizeIdentifierName(configuredName)
            ?? SanitizeIdentifierName(compilation.AssemblyName)
            ?? "Feature";
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
