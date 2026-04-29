using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Entitas.Generators.IntegrationTests;

public class GeneratedSurfaceTests
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

    [Fact]
    public void GeneratesCurrentSingleAssemblyOutputBuckets()
    {
        var result = RunGenerator();

        GetGeneratedFileNames(result).Should().Contain(new[]
        {
            "MainContext.g.cs",
            "MainMatcher.g.cs",
            "MainEntity.g.cs",
            "MainContextsExtension.g.cs",
            "MainContextsRegistrationExtension.g.cs",
            "MainComponentsLookup.g.cs",
            "MainEntityIndices.g.cs",
            "MainCleanupSystems.g.cs",
            "MainEventSystems.g.cs",
            "MyGame.MainHealthMatcherExtensions.g.cs",
            "MyGame.MainUniqueFlagMatcherExtensions.g.cs",
            "MyGame.MainCleanupMeMatcherExtensions.g.cs",
            "MyGame.MainReactiveMatcherExtensions.g.cs",
        });

        GetGeneratedSource(result, "MainContext.g.cs").Should().Contain("public MainMatcher Matcher => MainMatcher.Instance;");

        GetGeneratedFileNames(result).Should().Contain(fileName =>
            fileName.EndsWith("HealthComponent.g.cs", StringComparison.Ordinal));
        GetGeneratedFileNames(result).Should().Contain(fileName =>
            fileName.EndsWith("UniqueFlagComponent.g.cs", StringComparison.Ordinal));
        GetGeneratedFileNames(result).Should().Contain(fileName =>
            fileName.Contains("CleanupMe", StringComparison.Ordinal) && fileName.EndsWith("MainSystem.g.cs", StringComparison.Ordinal));
        GetGeneratedFileNames(result).Should().Contain(fileName =>
            fileName.Contains("Reactive", StringComparison.Ordinal) && fileName.EndsWith("EventSystem.g.cs", StringComparison.Ordinal));
        GetGeneratedFileNames(result).Should().Contain(fileName =>
            fileName.EndsWith("IMyGameAnyReactiveListener.g.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void PlainEntityApisUseComponentHandles()
    {
        var result = RunGenerator();

        var source = GetGeneratedSourceBySuffix(result, "HealthComponent.g.cs");

        source.Should().Contain("public static class MainHealthComponentHandle");
        source.Should().Contain("public static readonly Entitas.ComponentHandle<MyGame.HealthComponent> Handle = new Entitas.ComponentHandle<MyGame.HealthComponent>(\"MyGameHealth\");");
        source.Should().Contain("GetHealth(this MainEntity entity) { return (MyGame.HealthComponent)entity.GetComponent(MainHealthComponentHandle.Handle); }");
        source.Should().Contain("HasHealth(this MainEntity entity) { return entity.HasComponent(MainHealthComponentHandle.Handle); }");
        source.Should().Contain("var handle = MainHealthComponentHandle.Handle;");
        source.Should().Contain("entity.RemoveComponent(MainHealthComponentHandle.Handle);");
        source.Should().NotContain("entity.GetComponent(MainComponentsLookup.MyGameHealth)");
    }

    [Fact]
    public void UniqueContextApisUseComponentHandles()
    {
        var result = RunGenerator();

        var source = GetGeneratedSourceBySuffix(result, "UniqueFlagComponent.g.cs");

        source.Should().Contain("public static class MainUniqueFlagComponentHandle");
        source.Should().Contain("return entity.HasComponent(MainUniqueFlagComponentHandle.Handle);");
        source.Should().Contain("var handle = MainUniqueFlagComponentHandle.Handle;");
        source.Should().Contain("entity.AddComponent(handle, component);");
        source.Should().Contain("entity.RemoveComponent(handle);");
        source.Should().Contain("GetUniqueFlagEntity(this MainContext context) { return context.GetGroup(global::Entitas.Matcher<MainEntity>.AllOf(MainUniqueFlagComponentHandle.Handle)).GetSingleEntity(); }");
        source.Should().NotContain("MainMatcher.MyGameUniqueFlag()");
        source.Should().Contain("context.CreateEntity().SetUniqueFlag(true);");
    }

    [Fact]
    public void MatcherAndGlobalOutputsRemainLookupBased()
    {
        var result = RunGenerator();

        GetGeneratedSource(result, "MyGame.MainHealthMatcherExtensions.g.cs").Should().Contain(
            "global::Entitas.Matcher<MainEntity>.AllOf(MainComponentsLookup.MyGameHealth)");
        GetGeneratedSource(result, "MyGame.MainHealthMatcherExtensions.g.cs").Should().Contain(
            "generatedMatcher.ComponentNames = MainComponentsLookup.componentNames;");
        GetGeneratedSource(result, "MainComponentsLookup.g.cs").Should().Contain("public const int MyGameHealth = ");
        GetGeneratedSource(result, "MainComponentsLookup.g.cs").Should().Contain(
            "global::MyGame.MainHealthComponentHandle.Handle.AssignIndex(MyGameHealth);");
        GetGeneratedSource(result, "MainEntityIndices.g.cs").Should().Contain("MyGameHealth");
        GetGeneratedSource(result, "MainContextsRegistrationExtension.g.cs").Should().Contain("RegisterMain(this global::Entitas.Contexts contexts)");
        GetGeneratedSource(result, "MainContextsRegistrationExtension.g.cs").Should().Contain("InitializeMainEntityIndices(contexts);");
        GetGeneratedSource(result, "MainCleanupSystems.g.cs").Should().Contain("CleanupMe");
        GetGeneratedSource(result, "MainEventSystems.g.cs").Should().Contain("Reactive");
    }

    static GeneratorDriverRunResult RunGenerator() =>
        CodeGenerationTestHelper.RunGenerator(Source, "My.Gameplay");

    static string[] GetGeneratedFileNames(GeneratorDriverRunResult result) =>
        result.GeneratedTrees
            .Select(tree => Path.GetFileName(tree.FilePath))
            .ToArray();

    static string GetGeneratedSource(GeneratorDriverRunResult result, string fileName) =>
        result.GeneratedTrees
            .Single(tree => Path.GetFileName(tree.FilePath) == fileName)
            .GetText()
            .ToString();

    static string GetGeneratedSourceBySuffix(GeneratorDriverRunResult result, string fileNameSuffix) =>
        result.GeneratedTrees
            .Single(tree => Path.GetFileName(tree.FilePath).EndsWith(fileNameSuffix, StringComparison.Ordinal))
            .GetText()
            .ToString();
}
