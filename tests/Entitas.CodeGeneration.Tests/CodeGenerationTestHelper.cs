using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        Dictionary<string, string>? options = null,
        IEnumerable<MetadataReference>? additionalReferences = null)
    {
        var compilation = CreateCompilation(source, assemblyName, additionalReferences);

        var driver = CreateDriver(options);
        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    public static (GeneratorDriverRunResult Result, Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics)
        RunGeneratorAndUpdateCompilation(
            string source,
            string assemblyName,
            Dictionary<string, string>? options = null,
            IEnumerable<MetadataReference>? additionalReferences = null)
    {
        var compilation = CreateCompilation(source, assemblyName, additionalReferences);

        var driver = CreateDriver(options);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        return (driver.GetRunResult(), outputCompilation, diagnostics);
    }

    public static CSharpCompilation CreateCompilationFromSource(string source, string assemblyName) =>
        CreateCompilation(source, assemblyName, null);

    public static MetadataReference CreateReferenceFromCompilation(Compilation compilation)
    {
        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        if (!emitResult.Success)
        {
            var errors = string.Join("\n", emitResult.Diagnostics
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.ToString()));

            throw new InvalidOperationException(errors);
        }

        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    static CSharpCompilation CreateCompilation(
        string source,
        string assemblyName,
        IEnumerable<MetadataReference>? additionalReferences)
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Where(assembly => assembly.GetName().Name != typeof(CodeGenerationTestHelper).Assembly.GetName().Name)
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(Context<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Entitas.CodeGeneration.Attributes.ContextAttribute).Assembly.Location),
            })
            .Concat(additionalReferences ?? Enumerable.Empty<MetadataReference>())
            .Distinct(MetadataReferencePathComparer.Instance);

        return CSharpCompilation.Create(
            assemblyName,
            new[] { CSharpSyntaxTree.ParseText(source) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    static GeneratorDriver CreateDriver(Dictionary<string, string>? options)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new EntitasGenerator());

        if (options is not null)
            driver = driver.WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(options));

        return driver;
    }

    sealed class MetadataReferencePathComparer : IEqualityComparer<MetadataReference>
    {
        public static readonly MetadataReferencePathComparer Instance = new();

        public bool Equals(MetadataReference? x, MetadataReference? y)
        {
            var xPath = (x as PortableExecutableReference)?.FilePath;
            var yPath = (y as PortableExecutableReference)?.FilePath;

            if (string.IsNullOrEmpty(xPath) || string.IsNullOrEmpty(yPath))
                return ReferenceEquals(x, y);

            return StringComparer.OrdinalIgnoreCase.Equals(xPath, yPath);
        }

        public int GetHashCode([DisallowNull] MetadataReference obj)
        {
            var path = (obj as PortableExecutableReference)?.FilePath;
            return string.IsNullOrEmpty(path)
                ? RuntimeHelpers.GetHashCode(obj)
                : StringComparer.OrdinalIgnoreCase.GetHashCode(path);
        }
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
