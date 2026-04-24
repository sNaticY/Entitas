using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests;

public class GeneratedHintNameTests
{
    [Fact]
    public void NamespacedSingleContextComponentUsesShortHintName()
    {
        const string source = @"
using Entitas;
using Entitas.CodeGeneration.Attributes;

namespace Game.Feature
{
    public sealed class MainAttribute : ContextAttribute
    {
        public MainAttribute() : base(""Main"") { }
    }

    [Main]
    public sealed class UserComponent : IComponent { }
}
";

        var result = CodeGenerationTestHelper.RunGenerator(source, "Game.Feature");

        GetGeneratedFileNames(result).Should().Contain("Game.Feature.UserComponent.g.cs");
        GetGeneratedFileNames(result).Should().NotContain("Game.Feature.MainGameFeatureUserComponent.g.cs");
    }

    [Fact]
    public void MultiContextComponentHintNamesRemainDistinct()
    {
        const string source = @"
using Entitas;
using Entitas.CodeGeneration.Attributes;

namespace Game.Feature
{
    public sealed class MainAttribute : ContextAttribute
    {
        public MainAttribute() : base(""Main"") { }
    }

    public sealed class MetaAttribute : ContextAttribute
    {
        public MetaAttribute() : base(""Meta"") { }
    }

    [Main, Meta]
    public sealed class UserComponent : IComponent { }
}
";

        var result = CodeGenerationTestHelper.RunGenerator(source, "Game.Feature");

        GetGeneratedFileNames(result).Should().Contain("Game.Feature.Main.UserComponent.g.cs");
        GetGeneratedFileNames(result).Should().Contain("Game.Feature.Meta.UserComponent.g.cs");
    }

    static string[] GetGeneratedFileNames(Microsoft.CodeAnalysis.GeneratorDriverRunResult result) =>
        result.GeneratedTrees
            .Select(tree => Path.GetFileName(tree.FilePath))
            .ToArray();
}
