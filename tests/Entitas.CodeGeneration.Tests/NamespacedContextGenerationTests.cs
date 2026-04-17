using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests;

public class NamespacedContextGenerationTests
{
    const string Source = @"
namespace MyGame.Contexts.Markers
{
    public sealed class ClientUiContextMarkerAttribute : Entitas.CodeGeneration.Attributes.ContextAttribute
    {
        public ClientUiContextMarkerAttribute() : base(""Ui"") { }
    }
}
";

    [Fact]
    public void GeneratesContextSurfaceForNamespacedMarkerAttribute()
    {
        var result = CodeGenerationTestHelper.RunGenerator(Source, "My.Gameplay");

        GetGeneratedFileNames(result).Should().Contain(new[]
        {
            "UiContext.g.cs",
            "UiMatcher.g.cs",
            "UiEntity.g.cs",
            "UiContextsExtension.g.cs",
        });
    }

    [Fact]
    public void UsesConfiguredContextNameForNamespacedMarkerAccessor()
    {
        var result = CodeGenerationTestHelper.RunGenerator(Source, "My.Gameplay");

        var source = GetGeneratedSource(result, "UiContextsExtension.g.cs");
        source.Should().Contain("namespace Entitas");
        source.Should().Contain("public static class UiContextsExtension");
        source.Should().Contain("public static UiContext GetUi(this global::Entitas.Contexts contexts)");
        source.Should().NotContain("GetClientUiContextMarker(");
    }

    static string[] GetGeneratedFileNames(Microsoft.CodeAnalysis.GeneratorDriverRunResult result) =>
        result.GeneratedTrees
            .Select(tree => Path.GetFileName(tree.FilePath))
            .ToArray();

    static string GetGeneratedSource(Microsoft.CodeAnalysis.GeneratorDriverRunResult result, string fileName) =>
        result.GeneratedTrees
            .Single(tree => Path.GetFileName(tree.FilePath) == fileName)
            .GetText()
            .ToString();
}
