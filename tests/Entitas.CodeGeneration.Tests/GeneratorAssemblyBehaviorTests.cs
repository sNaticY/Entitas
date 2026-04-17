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

    const string ConfigContextSource = @"
namespace MyGame.Configuration
{
    public sealed class ConfigAttribute : Entitas.CodeGeneration.Attributes.ContextAttribute
    {
        public ConfigAttribute() : base(""Config"") { }
    }
}
";

    const string AliasedContextSource = @"
namespace MyGame.Aliases
{
    public sealed class GameplayContextMarkerAttribute : Entitas.CodeGeneration.Attributes.ContextAttribute
    {
        public GameplayContextMarkerAttribute() : base(""Main"") { }
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

    [Fact]
    public void GeneratesWhenAssemblyIsIncludedInMultiAssemblyFilter()
    {
        var result = CodeGenerationTestHelper.RunGenerator(
            ContextSource,
            "My.Gameplay",
            new Dictionary<string, string>
            {
                ["entitas_generator.assembly_names"] = "Assembly-CSharp; My.Gameplay; My.Configuration"
            });

        result.GeneratedTrees.Select(tree => Path.GetFileName(tree.FilePath)).Should().Contain(new[]
        {
            "MainContext.g.cs",
            "MainMatcher.g.cs",
            "MainEntity.g.cs",
            "MainContextsExtension.g.cs",
        });
    }

    [Fact]
    public void SkipsCompilationsNotIncludedInMultiAssemblyFilter()
    {
        var gameplayResult = CodeGenerationTestHelper.RunGenerator(
            ContextSource,
            "My.Gameplay",
            new Dictionary<string, string>
            {
                ["entitas_generator.assembly_names"] = "Assembly-CSharp,My.Configuration"
            });

        var configurationResult = CodeGenerationTestHelper.RunGenerator(
            ConfigContextSource,
            "My.Configuration",
            new Dictionary<string, string>
            {
                ["entitas_generator.assembly_names"] = "Assembly-CSharp,My.Configuration"
            });

        gameplayResult.GeneratedTrees.Should().BeEmpty();
        configurationResult.GeneratedTrees.Select(tree => Path.GetFileName(tree.FilePath)).Should().Contain(new[]
        {
            "ConfigContext.g.cs",
            "ConfigMatcher.g.cs",
            "ConfigEntity.g.cs",
            "ConfigContextsExtension.g.cs",
        });
    }

    [Fact]
    public void GeneratesIndependentContextSurfacesPerCompilation()
    {
        var gameplayResult = CodeGenerationTestHelper.RunGenerator(ContextSource, "My.Gameplay");
        var configurationResult = CodeGenerationTestHelper.RunGenerator(ConfigContextSource, "My.Configuration");

        var gameplayFiles = gameplayResult.GeneratedTrees.Select(tree => Path.GetFileName(tree.FilePath)).ToArray();
        gameplayFiles.Should().Contain(new[]
        {
            "MainContext.g.cs",
            "MainMatcher.g.cs",
            "MainEntity.g.cs",
            "MainContextsExtension.g.cs",
        });
        gameplayFiles.Should().NotContain(new[]
        {
            "ConfigContext.g.cs",
            "ConfigMatcher.g.cs",
            "ConfigEntity.g.cs",
            "ConfigContextsExtension.g.cs",
        });

        var configurationFiles = configurationResult.GeneratedTrees.Select(tree => Path.GetFileName(tree.FilePath)).ToArray();
        configurationFiles.Should().Contain(new[]
        {
            "ConfigContext.g.cs",
            "ConfigMatcher.g.cs",
            "ConfigEntity.g.cs",
            "ConfigContextsExtension.g.cs",
        });
        configurationFiles.Should().NotContain(new[]
        {
            "MainContext.g.cs",
            "MainMatcher.g.cs",
            "MainEntity.g.cs",
            "MainContextsExtension.g.cs",
        });
    }

    [Fact]
    public void UsesConfiguredContextNameInsteadOfAttributeClassName()
    {
        var result = CodeGenerationTestHelper.RunGenerator(AliasedContextSource, "My.Gameplay");

        var files = result.GeneratedTrees.Select(tree => Path.GetFileName(tree.FilePath)).ToArray();
        files.Should().Contain(new[]
        {
            "MainContext.g.cs",
            "MainMatcher.g.cs",
            "MainEntity.g.cs",
            "MainContextsExtension.g.cs",
        });
        files.Should().NotContain(new[]
        {
            "GameplayContextMarkerContext.g.cs",
            "GameplayContextMarkerMatcher.g.cs",
            "GameplayContextMarkerEntity.g.cs",
            "GameplayContextMarkerContextsExtension.g.cs",
        });
    }
}
