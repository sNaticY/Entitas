using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Entitas.CodeGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Entitas.Generators.IntegrationTests;

static class CodeGenerationTestHelper
{
    public static GeneratorDriverRunResult RunGenerator(
        string source,
        string assemblyName,
        Dictionary<string, string>? options = null)
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(Context<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Entitas.CodeGeneration.Attributes.ContextAttribute).Assembly.Location),
            })
            .Distinct(MetadataReferencePathComparer.Instance);

        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { CSharpSyntaxTree.ParseText(source) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new EntitasGenerator());

        if (options is not null)
            driver = driver.WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(options));

        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    sealed class MetadataReferencePathComparer : IEqualityComparer<MetadataReference>
    {
        public static readonly MetadataReferencePathComparer Instance = new();

        public bool Equals(MetadataReference? x, MetadataReference? y) =>
            StringComparer.OrdinalIgnoreCase.Equals((x as PortableExecutableReference)?.FilePath, (y as PortableExecutableReference)?.FilePath);

        public int GetHashCode([DisallowNull] MetadataReference obj) =>
            StringComparer.OrdinalIgnoreCase.GetHashCode((obj as PortableExecutableReference)?.FilePath ?? string.Empty);
    }

    sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        readonly AnalyzerConfigOptions _options;

        public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> options)
        {
            _options = new DictionaryAnalyzerConfigOptions(options.ToImmutableDictionary());
        }

        public override AnalyzerConfigOptions GlobalOptions => _options;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;
    }

    sealed class DictionaryAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        readonly ImmutableDictionary<string, string> _options;

        public DictionaryAnalyzerConfigOptions(ImmutableDictionary<string, string> options)
        {
            _options = options;
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) =>
            _options.TryGetValue(key, out value);
    }
}
