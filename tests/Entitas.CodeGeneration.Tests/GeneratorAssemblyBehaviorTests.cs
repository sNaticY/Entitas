using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests;

public class GeneratorAssemblyBehaviorTests
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
    public void GeneratesContextsForCustomAssemblyByDefault()
    {
        var result = CodeGenerationTestHelper.RunGenerator(ContextSource, "My.Gameplay");

        result.GeneratedTrees.Select(tree => Path.GetFileName(tree.FilePath)).Should().Contain(new[]
        {
            "MainContext.g.cs",
            "MainMatcher.g.cs",
            "MainEntity.g.cs",
            "MainContextsExtension.g.cs",
        });
    }

    [Fact]
    public void DoesNotGenerateAnythingForCompilationWithoutContexts()
    {
        var result = CodeGenerationTestHelper.RunGenerator("public sealed class PlainType { }", "Unrelated.Assembly");

        result.GeneratedTrees.Should().BeEmpty();
    }

    [Fact]
    public void RespectsConfiguredAssemblyFilter()
    {
        var result = CodeGenerationTestHelper.RunGenerator(
            ContextSource,
            "My.Gameplay",
            new Dictionary<string, string>
            {
                ["entitas_generator.assembly_names"] = "Assembly-CSharp"
            });

        result.GeneratedTrees.Should().BeEmpty();
    }
}
