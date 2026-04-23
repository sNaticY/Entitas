using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Entitas.Generators.IntegrationTests;

public class FeatureOwnedComponentApiTests
{
    const string RootSource = @"
namespace Game.Root
{
    public sealed class MainAttribute : Entitas.CodeGeneration.Attributes.ContextAttribute
    {
        public MainAttribute() : base(""Main"") { }
    }
}
";

    const string FeatureSource = @"
using Entitas;
using Entitas.CodeGeneration.Attributes;
using Game.Root;

namespace Game.Feature
{
    [Main, Unique]
    public sealed class UserComponent : IComponent
    {
        public string Name;
        public int Age;
    }

    [Main]
    public sealed class LoadingComponent : IComponent { }

    [Main]
    [Event(EventTarget.Any)]
    public sealed class ReactiveComponent : IComponent
    {
        public int Value;
    }

    [Main]
    [Cleanup(CleanupMode.RemoveComponent)]
    public sealed class CleanupMeComponent : IComponent { }
}
";

    [Fact]
    public void FeatureAssemblyGeneratesPlainComponentApisWithoutContextGlobalOutputs()
    {
        var root = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(RootSource, "Game.Root");
        AssertNoErrors(root.Diagnostics.Concat(root.Compilation.GetDiagnostics()));
        var rootReference = CodeGenerationTestHelper.CreateReferenceFromCompilation(root.Compilation);

        var feature = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(
            FeatureSource,
            "Game.Feature",
            additionalReferences: new[] { rootReference });

        AssertNoErrors(feature.Diagnostics.Concat(feature.Compilation.GetDiagnostics()));
        GetGeneratedFileNames(feature.Result).Should().Contain(fileName =>
            fileName.EndsWith("UserComponent.g.cs", StringComparison.Ordinal));
        GetGeneratedFileNames(feature.Result).Should().Contain(fileName =>
            fileName.EndsWith("LoadingComponent.g.cs", StringComparison.Ordinal));
        GetGeneratedFileNames(feature.Result).Should().NotContain("MainComponentsLookup.g.cs");
        GetGeneratedFileNames(feature.Result).Should().NotContain("MainMatcher.g.cs");
        GetGeneratedFileNames(feature.Result).Should().NotContain("MainEventSystems.g.cs");
        GetGeneratedFileNames(feature.Result).Should().NotContain("MainCleanupSystems.g.cs");
        GetGeneratedFileNames(feature.Result).Should().NotContain(fileName =>
            fileName.EndsWith("Matcher.g.cs", StringComparison.Ordinal));

        var userSource = GetGeneratedSourceBySuffix(feature.Result, "UserComponent.g.cs");
        userSource.Should().Contain("public static class MainUserComponentHandle");
        userSource.Should().Contain("AddUser(this MainEntity entity, string newName, int newAge)");
        userSource.Should().Contain("SetUser(this MainContext context, string newName, int newAge)");
        userSource.Should().Contain("global::Entitas.Matcher<MainEntity>.AllOf(MainUserComponentHandle.Handle)");
        userSource.Should().NotContain("MainComponentsLookup");
        userSource.Should().NotContain("MainMatcher");

        var loadingSource = GetGeneratedSourceBySuffix(feature.Result, "LoadingComponent.g.cs");
        loadingSource.Should().Contain("public static class MainLoadingComponentHandle");
        loadingSource.Should().Contain("SetLoading(this MainEntity entity, bool value)");
        loadingSource.Should().NotContain("MainComponentsLookup");

        GetGeneratedFileNames(feature.Result).Should().Contain(fileName =>
            fileName.Contains("Reactive", StringComparison.Ordinal) && fileName.EndsWith("EventSystem.g.cs", StringComparison.Ordinal));
        GetGeneratedFileNames(feature.Result).Should().Contain(fileName =>
            fileName.Contains("Reactive", StringComparison.Ordinal) && fileName.Contains(".I", StringComparison.Ordinal));
        GetGeneratedFileNames(feature.Result).Should().Contain(fileName =>
            fileName.Contains("CleanupMe", StringComparison.Ordinal) && fileName.EndsWith("MainSystem.g.cs", StringComparison.Ordinal));

        GetGeneratedSources(feature.Result, fileName => fileName.Contains("Reactive", StringComparison.Ordinal))
            .Should().Contain(source => source.Contains("global::Entitas.Matcher<MainEntity>.AllOf(MainReactiveComponentHandle.Handle)", StringComparison.Ordinal));
        GetGeneratedSources(feature.Result, fileName => fileName.Contains("CleanupMe", StringComparison.Ordinal))
            .Should().Contain(source => source.Contains("global::Entitas.Matcher<MainEntity>.AllOf(MainCleanupMeComponentHandle.Handle)", StringComparison.Ordinal));
    }

    static void AssertNoErrors(IEnumerable<Diagnostic> diagnostics) =>
        diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should()
            .BeEmpty();

    static string[] GetGeneratedFileNames(GeneratorDriverRunResult result) =>
        result.GeneratedTrees
            .Select(tree => Path.GetFileName(tree.FilePath))
            .ToArray();

    static string GetGeneratedSourceBySuffix(GeneratorDriverRunResult result, string fileNameSuffix) =>
        result.GeneratedTrees
            .Single(tree => Path.GetFileName(tree.FilePath).EndsWith(fileNameSuffix, StringComparison.Ordinal))
            .GetText()
            .ToString();

    static string[] GetGeneratedSources(GeneratorDriverRunResult result, Func<string, bool> predicate) =>
        result.GeneratedTrees
            .Where(tree => predicate(Path.GetFileName(tree.FilePath)))
            .Select(tree => tree.GetText().ToString())
            .ToArray();
}
