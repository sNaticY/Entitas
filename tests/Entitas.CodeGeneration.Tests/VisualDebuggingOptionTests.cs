using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests;

public class VisualDebuggingOptionTests
{
    const string ContextSource = @"
namespace MyGame
{
    public sealed class MainAttribute : Entitas.CodeGeneration.Attributes.ContextAttribute
    {
        public MainAttribute() : base(""Main"") { }
    }
}
";

    [Fact]
    public void GeneratesVisualDebuggingForAssemblyCSharpByDefault()
    {
        var result = CodeGenerationTestHelper.RunGenerator(ContextSource, "Assembly-CSharp");

        GetGeneratedFileNames(result).Should().Contain(new[]
        {
            "Feature.g.cs",
            "ContextObservers.g.cs",
        });
    }

    [Fact]
    public void DoesNotGenerateVisualDebuggingForCustomAssemblyByDefault()
    {
        var result = CodeGenerationTestHelper.RunGenerator(ContextSource, "My.Gameplay");

        GetGeneratedFileNames(result).Should().NotContain(new[]
        {
            "Feature.g.cs",
            "ContextObservers.g.cs",
        });
    }

    [Fact]
    public void RespectsVisualDebuggingToggle()
    {
        var result = CodeGenerationTestHelper.RunGenerator(
            ContextSource,
            "Assembly-CSharp",
            new Dictionary<string, string>
            {
                ["entitas_generator.visual_debugging"] = "false"
            });

        GetGeneratedFileNames(result).Should().NotContain(new[]
        {
            "Feature.g.cs",
            "ContextObservers.g.cs",
        });
    }

    [Fact]
    public void RespectsVisualDebuggingAssemblyFilter()
    {
        var result = CodeGenerationTestHelper.RunGenerator(
            ContextSource,
            "My.Gameplay",
            new Dictionary<string, string>
            {
                ["entitas_generator.visual_debugging.assembly_names"] = "My.Gameplay"
            });

        GetGeneratedFileNames(result).Should().Contain(new[]
        {
            "Feature.g.cs",
            "ContextObservers.g.cs",
        });
    }

    static string[] GetGeneratedFileNames(Microsoft.CodeAnalysis.GeneratorDriverRunResult result) =>
        result.GeneratedTrees
            .Select(tree => Path.GetFileName(tree.FilePath))
            .ToArray();
}
