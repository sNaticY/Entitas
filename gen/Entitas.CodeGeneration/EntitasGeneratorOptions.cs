using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Entitas.CodeGeneration;

readonly struct EntitasGeneratorOptions
{
    const string AssemblyNamesKey = "entitas_generator.assembly_names";
    const string VisualDebuggingKey = "entitas_generator.visual_debugging";
    const string VisualDebuggingAssemblyNamesKey = "entitas_generator.visual_debugging.assembly_names";

    static readonly ImmutableHashSet<string> DefaultVisualDebuggingAssemblyNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Assembly-CSharp");

    public EntitasGeneratorOptions(
        ImmutableHashSet<string>? assemblyNames,
        ImmutableHashSet<string> visualDebuggingAssemblyNames,
        bool visualDebuggingGenerationEnabled)
    {
        AssemblyNames = assemblyNames;
        VisualDebuggingAssemblyNames = visualDebuggingAssemblyNames;
        VisualDebuggingGenerationEnabled = visualDebuggingGenerationEnabled;
    }

    public ImmutableHashSet<string>? AssemblyNames { get; }
    public ImmutableHashSet<string> VisualDebuggingAssemblyNames { get; }
    public bool VisualDebuggingGenerationEnabled { get; }

    public static EntitasGeneratorOptions From(AnalyzerConfigOptionsProvider optionsProvider, Compilation compilation)
    {
        var syntaxTree = compilation.SyntaxTrees.FirstOrDefault();
        var options = syntaxTree is not null
            ? optionsProvider.GetOptions(syntaxTree)
            : optionsProvider.GlobalOptions;

        return new EntitasGeneratorOptions(
            GetAssemblyNames(options, AssemblyNamesKey),
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
}
