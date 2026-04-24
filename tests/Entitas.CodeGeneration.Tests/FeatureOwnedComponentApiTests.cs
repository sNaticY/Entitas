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
using Entitas.CodeGeneration.Attributes;

[assembly: EntitasAssembly(""Player"")]

using Entitas;
using Game.Root;

namespace Game.Feature
{
    [Main, Unique]
    public sealed class UserComponent : IComponent
    {
        [PrimaryEntityIndex]
        public string Name;

        [EntityIndex]
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
        GetGeneratedSource(root.Result, "MainContext.g.cs").Should().Contain("public MainContext(global::Entitas.ContextSchema schema)");
        GetGeneratedSource(root.Result, "MainContext.g.cs").Should().Contain("public static global::Entitas.ContextSchemaBuilder CreateSchemaBuilder()");
        GetGeneratedSource(root.Result, "MainContext.g.cs").Should().Contain("return new global::Entitas.ContextSchemaBuilder(\"Main\");");
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
        GetGeneratedFileNames(feature.Result).Should().NotContain("MainEntityIndices.g.cs");
        GetGeneratedFileNames(feature.Result).Should().NotContain(fileName =>
            fileName.EndsWith("Matcher.g.cs", StringComparison.Ordinal));

        var userSource = GetGeneratedSourceBySuffix(feature.Result, "UserComponent.g.cs");
        userSource.Should().Contain("public static class MainUserComponentHandle");
        userSource.Should().Contain("AddMainUser(this global::Entitas.ContextSchemaBuilder builder)");
        userSource.Should().Contain("AddUser(this MainEntity entity, string newName, int newAge)");
        userSource.Should().Contain("SetUser(this MainContext context, string newName, int newAge)");
        userSource.Should().Contain("global::Entitas.Matcher<MainEntity>.AllOf(MainUserComponentHandle.Handle)");
        userSource.Should().NotContain("MainComponentsLookup");
        userSource.Should().NotContain("MainMatcher");

        var indexSource = GetGeneratedSourceBySuffix(feature.Result, "UserEntityIndices.g.cs");
        indexSource.Should().Contain("public static class MainUserEntityIndices");
        indexSource.Should().Contain("AddMainUserEntityIndices(this global::Entitas.ContextSchemaBuilder builder)");
        indexSource.Should().Contain("builder.AddEntityIndex(MainUserEntityIndices.GameFeatureUserName");
        indexSource.Should().Contain("global::Entitas.Matcher<MainEntity>.AllOf(MainUserComponentHandle.Handle)");
        indexSource.Should().Contain("GetEntityWithUserName(this MainContext context, string Name)");
        indexSource.Should().Contain("GetEntitiesWithUserAge(this MainContext context, int Age)");
        indexSource.Should().NotContain("GetEntityWithGameFeatureUserName");
        indexSource.Should().NotContain("GetEntitiesWithGameFeatureUserAge");
        indexSource.Should().NotContain("MainEntityIndices");
        indexSource.Should().NotContain("MainMatcher");

        var loadingSource = GetGeneratedSourceBySuffix(feature.Result, "LoadingComponent.g.cs");
        loadingSource.Should().Contain("public static class MainLoadingComponentHandle");
        loadingSource.Should().Contain("AddMainLoading(this global::Entitas.ContextSchemaBuilder builder)");
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
        GetGeneratedSources(feature.Result, fileName => fileName.Contains("Reactive", StringComparison.Ordinal))
            .Should().Contain(source => source.Contains("builder.AddEventSystem", StringComparison.Ordinal));
        GetGeneratedSources(feature.Result, fileName => fileName.Contains("CleanupMe", StringComparison.Ordinal))
            .Should().Contain(source => source.Contains("global::Entitas.Matcher<MainEntity>.AllOf(MainCleanupMeComponentHandle.Handle)", StringComparison.Ordinal));
        GetGeneratedSources(feature.Result, fileName => fileName.Contains("CleanupMe", StringComparison.Ordinal))
            .Should().Contain(source => source.Contains("builder.AddCleanupSystem", StringComparison.Ordinal));

        var assemblySchemaSource = GetGeneratedSource(feature.Result, "MainPlayerAssemblySchemaExtensions.g.cs");
        assemblySchemaSource.Should().Contain("public static global::Entitas.ContextSchemaBuilder AddPlayerAssembly(this global::Entitas.ContextSchemaBuilder builder)");
        assemblySchemaSource.Should().Contain("builder = global::Game.Feature.MainUserComponentSchemaExtensions.AddMainUser(builder);");
        assemblySchemaSource.Should().Contain("builder = global::Game.Feature.MainUserEntityIndicesSchemaExtensions.AddMainUserEntityIndices(builder);");
        assemblySchemaSource.Should().Contain("builder = global::Game.Feature.MainReactiveComponentSchemaExtensions.AddMainReactive(builder);");
        assemblySchemaSource.Should().Contain("builder = global::Game.Feature.AnyReactiveAddedEventSystemSchemaExtensions.AddAnyReactiveAddedEventSystem(builder);");
        assemblySchemaSource.Should().Contain("builder = global::Game.Feature.RemoveGameFeatureCleanupMeMainSystemSchemaExtensions.AddRemoveGameFeatureCleanupMeMainSystem(builder);");
    }

    [Fact]
    public void AssemblyFallsBackToSanitizedAssemblyNameForAssemblyRegistration()
    {
        var root = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(RootSource, "Game.Root");
        AssertNoErrors(root.Diagnostics.Concat(root.Compilation.GetDiagnostics()));
        var rootReference = CodeGenerationTestHelper.CreateReferenceFromCompilation(root.Compilation);

        var feature = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(
            FeatureSource.Replace("[assembly: EntitasAssembly(\"Player\")]", string.Empty),
            "Game.Feature-Fallback",
            additionalReferences: new[] { rootReference });

        AssertNoErrors(feature.Diagnostics.Concat(feature.Compilation.GetDiagnostics()));
        GetGeneratedFileNames(feature.Result).Should().Contain("MainGameFeatureFallbackAssemblySchemaExtensions.g.cs");
        GetGeneratedSource(feature.Result, "MainGameFeatureFallbackAssemblySchemaExtensions.g.cs")
            .Should().Contain("AddGameFeatureFallbackAssembly(this global::Entitas.ContextSchemaBuilder builder)");
    }

    [Fact]
    public void AssemblyGeneratesContextSpecificAssemblyMethodsForMultipleContexts()
    {
        const string rootSource = @"
namespace Game.Root
{
    public sealed class MainAttribute : Entitas.CodeGeneration.Attributes.ContextAttribute
    {
        public MainAttribute() : base(""Main"") { }
    }

    public sealed class MetaAttribute : Entitas.CodeGeneration.Attributes.ContextAttribute
    {
        public MetaAttribute() : base(""Meta"") { }
    }
}
";

        const string featureSource = @"
using Entitas;
using Entitas.CodeGeneration.Attributes;
using Game.Root;

[assembly: EntitasAssembly(""Player"")]

namespace Game.Feature
{
    [Main]
    public sealed class UserComponent : IComponent { }

    [Meta]
    public sealed class SettingsComponent : IComponent { }
}
";

        var root = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(rootSource, "Game.Root");
        AssertNoErrors(root.Diagnostics.Concat(root.Compilation.GetDiagnostics()));
        var rootReference = CodeGenerationTestHelper.CreateReferenceFromCompilation(root.Compilation);

        var feature = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(
            featureSource,
            "Game.Feature",
            additionalReferences: new[] { rootReference });

        AssertNoErrors(feature.Diagnostics.Concat(feature.Compilation.GetDiagnostics()));
        GetGeneratedSource(feature.Result, "MainPlayerAssemblySchemaExtensions.g.cs")
            .Should().Contain("AddMainPlayerAssembly(this global::Entitas.ContextSchemaBuilder builder)");
        GetGeneratedSource(feature.Result, "MetaPlayerAssemblySchemaExtensions.g.cs")
            .Should().Contain("AddMetaPlayerAssembly(this global::Entitas.ContextSchemaBuilder builder)");
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

    static string GetGeneratedSource(GeneratorDriverRunResult result, string fileName) =>
        result.GeneratedTrees
            .Single(tree => Path.GetFileName(tree.FilePath) == fileName)
            .GetText()
            .ToString();

    static string[] GetGeneratedSources(GeneratorDriverRunResult result, Func<string, bool> predicate) =>
        result.GeneratedTrees
            .Where(tree => predicate(Path.GetFileName(tree.FilePath)))
            .Select(tree => tree.GetText().ToString())
            .ToArray();
}
