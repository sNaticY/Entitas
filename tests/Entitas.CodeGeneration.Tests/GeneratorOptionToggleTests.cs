using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests;

public class GeneratorOptionToggleTests
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
    public sealed class UniqueFlagComponent : IComponent { }

    [Main]
    public sealed class HealthComponent : IComponent
    {
        [PrimaryEntityIndex]
        public string Id;
        public int Value;
    }

    [Main]
    [Cleanup(CleanupMode.RemoveComponent)]
    public sealed class CleanupMeComponent : IComponent { }

    [Main]
    [Event(EventTarget.Any)]
    public sealed class ReactiveComponent : IComponent
    {
        public int Value;
    }
}
";

    [Theory]
    [InlineData("entitas_generator.context.context", "MainContext.g.cs")]
    [InlineData("entitas_generator.context.matcher", "MainMatcher.g.cs")]
    [InlineData("entitas_generator.context.entity", "MainEntity.g.cs")]
    [InlineData("entitas_generator.component.event_systems_extension", "MainEventSystems.g.cs")]
    [InlineData("entitas_generator.component.entity_index_extension", "MainEntityIndices.g.cs")]
    [InlineData("entitas_generator.component.cleanup_systems", "MainCleanupSystems.g.cs")]
    public void DisablesStandaloneGeneratedFiles(string optionKey, string fileName)
    {
        var result = RunWithOption(optionKey, false);

        GetGeneratedFileNames(result).Should().NotContain(fileName);
    }

    [Theory]
    [InlineData("entitas_generator.component.component_index")]
    [InlineData("entitas_generator.context.component_index")]
    public void DisablesComponentLookupWhenEitherComponentIndexToggleIsOff(string optionKey)
    {
        var result = RunWithOption(optionKey, false);

        GetGeneratedFileNames(result).Should().NotContain("MainComponentsLookup.g.cs");
    }

    [Fact]
    public void DisablesPerComponentEventArtifacts()
    {
        var result = RunWithOption("entitas_generator.component.events", false);

        GetGeneratedFileNames(result).Should().NotContain(fileName => fileName.Contains("AnyMyGameReactiveAdded", System.StringComparison.Ordinal));
        GetGeneratedFileNames(result).Should().NotContain("IAnyMyGameReactiveAddedListener.g.cs");
    }

    [Fact]
    public void DisablesPerComponentCleanupSystemArtifacts()
    {
        var result = RunWithOption("entitas_generator.component.cleanup_systems", false);

        GetGeneratedFileNames(result).Should().NotContain("RemoveMyGameCleanupMeMainSystem.g.cs");
    }

    [Fact]
    public void DisablesComponentContextExtensionsInsideSharedComponentFile()
    {
        var result = RunWithOption("entitas_generator.component.context_extension", false);

        GetGeneratedSourceBySuffix(result, "UniqueFlagComponent.g.cs").Should().NotContain("public static class MainMyGameUniqueFlagContextExtensions");
        GetGeneratedFileNames(result).Should().NotContain("MainContextsExtension.g.cs");
    }

    [Fact]
    public void DisablesComponentEntityExtensionsInsideSharedComponentFile()
    {
        var result = RunWithOption("entitas_generator.component.entity_extension", false);

        var source = GetGeneratedSourceBySuffix(result, "HealthComponent.g.cs");

        source.Should().NotContain("public static class MainMyGameHealthEntityExtensions");
        source.Should().NotContain("AddMyGameHealth(this MainEntity entity");
    }

    [Fact]
    public void DisablesComponentMatcherExtensionsInsideSharedComponentFile()
    {
        var result = RunWithOption("entitas_generator.component.matcher", false);

        var source = GetGeneratedSourceBySuffix(result, "HealthComponent.g.cs");

        source.Should().NotContain("public sealed partial class MainMatcher");
        source.Should().NotContain("public static Entitas.IMatcher<MainEntity> MyGameHealth()");
    }

    static Microsoft.CodeAnalysis.GeneratorDriverRunResult RunWithOption(string optionKey, bool value) =>
        CodeGenerationTestHelper.RunGenerator(
            Source,
            "My.Gameplay",
            new Dictionary<string, string>
            {
                [optionKey] = value.ToString().ToLowerInvariant()
            });

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
