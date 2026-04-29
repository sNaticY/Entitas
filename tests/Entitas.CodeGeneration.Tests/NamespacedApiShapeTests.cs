using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests;

public class NamespacedApiShapeTests
{
    const string Source = @"
using Entitas;
using Entitas.CodeGeneration.Attributes;

namespace MyGame
{
    public sealed class MainAttribute : ContextAttribute
    {
        public MainAttribute() : base(""Main"") { }
    }

    [Main, Unique]
    public sealed class UserComponent : IComponent
    {
        public string Name;
        public int Age;
    }

    [Main, Unique]
    public sealed class LoadingComponent : IComponent { }
}
";

    [Fact]
    public void GeneratesShortEntityApiInsideComponentNamespace()
    {
        var result = CodeGenerationTestHelper.RunGenerator(Source, "My.Gameplay");

        var source = GetGeneratedSourceBySuffix(result, "UserComponent.g.cs");

        source.Should().Contain("namespace MyGame");
        source.Should().Contain("public static class MainUserEntityExtensions");
        source.Should().Contain("AddUser(this MainEntity entity, string newName, int newAge)");
        source.Should().Contain("ReplaceUser(this MainEntity entity, string newName, int newAge)");
        source.Should().Contain("GetUser(this MainEntity entity)");
        source.Should().NotContain("AddMyGameUser(");
        source.Should().NotContain("ReplaceMyGameUser(");
        source.Should().NotContain("GetMyGameUser(");
    }

    [Fact]
    public void GeneratesShortContextApiInsideComponentNamespace()
    {
        var result = CodeGenerationTestHelper.RunGenerator(Source, "My.Gameplay");

        var source = GetGeneratedSourceBySuffix(result, "UserComponent.g.cs");

        source.Should().Contain("public static class MainUserContextExtensions");
        source.Should().Contain("SetUser(this MainContext context, string newName, int newAge)");
        source.Should().Contain("ReplaceUser(this MainContext context, string newName, int newAge)");
        source.Should().Contain("GetUserEntity(this MainContext context)");
        source.Should().Contain("global::Entitas.Matcher<MainEntity>.AllOf(MainUserComponentHandle.Handle)");
        source.Should().NotContain("MainMatcher.MyGameUser()");
        source.Should().NotContain("SetMyGameUser(");
        source.Should().NotContain("ReplaceMyGameUser(");
        source.Should().NotContain("GetMyGameUserEntity(");
    }

    [Fact]
    public void EmitsMatcherExtensionInsideComponentNamespace()
    {
        var result = CodeGenerationTestHelper.RunGenerator(Source, "My.Gameplay");

        GetGeneratedFileNames(result).Should().Contain("MyGame.MainUserMatcherExtensions.g.cs");

        var source = GetGeneratedSource(result, "MyGame.MainUserMatcherExtensions.g.cs");
        source.Should().Contain("namespace MyGame");
        source.Should().Contain("public static class MainUserMatcherExtensions");
        source.Should().Contain("public static global::Entitas.IMatcher<MainEntity> User(this MainMatcher matcher)");
        source.Should().NotContain("MyGameUser(");
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

    static string GetGeneratedSourceBySuffix(Microsoft.CodeAnalysis.GeneratorDriverRunResult result, string fileNameSuffix) =>
        result.GeneratedTrees
            .Single(tree => Path.GetFileName(tree.FilePath).EndsWith(fileNameSuffix, System.StringComparison.Ordinal))
            .GetText()
            .ToString();
}
