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

[assembly: EntitasAssembly(""Player"")]

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
        GetGeneratedFileNames(feature.Result).Should().Contain(fileName =>
            fileName.EndsWith("MainPlayerMatcher.g.cs", StringComparison.Ordinal));
        GetGeneratedFileNames(feature.Result).Should().Contain(fileName =>
            fileName.EndsWith("MainPlayerMatcher.User.g.cs", StringComparison.Ordinal));
        GetGeneratedFileNames(feature.Result).Should().NotContain(fileName =>
            fileName.EndsWith("MainUserMatcher.g.cs", StringComparison.Ordinal));

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

        var matcherSource = GetGeneratedSourceBySuffix(feature.Result, "MainPlayerMatcher.User.g.cs");
        matcherSource.Should().Contain("public static partial class MainPlayerMatcher");
        matcherSource.Should().Contain("public static global::Entitas.IMatcher<MainEntity> User()");
        matcherSource.Should().Contain("global::Entitas.Matcher<MainEntity>.AllOf(global::Game.Feature.MainUserComponentHandle.Handle)");
        matcherSource.Should().NotContain("MainComponentsLookup");
        matcherSource.Should().NotContain("MainMatcher");

        var loadingMatcherSource = GetGeneratedSourceBySuffix(feature.Result, "MainPlayerMatcher.Loading.g.cs");
        loadingMatcherSource.Should().Contain("public static partial class MainPlayerMatcher");
        loadingMatcherSource.Should().Contain("public static global::Entitas.IMatcher<MainEntity> Loading()");
        loadingMatcherSource.Should().Contain("global::Entitas.Matcher<MainEntity>.AllOf(global::Game.Feature.MainLoadingComponentHandle.Handle)");

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
        assemblySchemaSource.Should().Contain("builder = global::Game.Feature.GameFeatureAnyReactiveEventSystemSchemaExtensions.AddGameFeatureAnyReactiveEventSystem(builder);");
        assemblySchemaSource.Should().Contain("builder = global::Game.Feature.RemoveGameFeatureCleanupMeMainSystemSchemaExtensions.AddRemoveGameFeatureCleanupMeMainSystem(builder);");
    }

    [Fact]
    public void FeatureOwnedMatcherApisCompileInReactiveSystems()
    {
        var root = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(RootSource, "Game.Root");
        AssertNoErrors(root.Diagnostics.Concat(root.Compilation.GetDiagnostics()));
        var rootReference = CodeGenerationTestHelper.CreateReferenceFromCompilation(root.Compilation);

        var feature = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(
            FeatureSource,
            "Game.Feature",
            additionalReferences: new[] { rootReference });
        AssertNoErrors(feature.Diagnostics.Concat(feature.Compilation.GetDiagnostics()));
        var featureReference = CodeGenerationTestHelper.CreateReferenceFromCompilation(feature.Compilation);

        const string systemSource = @"
using System.Collections.Generic;
using Entitas;
using Game.Feature;

public sealed class UserAddedSystem : ReactiveSystem<MainEntity>
{
    public UserAddedSystem(MainContext context) : base(context) { }

    protected override ICollector<MainEntity> GetTrigger(IContext<MainEntity> context)
    {
        return context.CreateCollector(MainPlayerMatcher.User().Added());
    }

    protected override bool Filter(MainEntity entity)
    {
        return entity.HasUser();
    }

    protected override void Execute(List<MainEntity> entities)
    {
    }
}
";

        var system = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(
            systemSource,
            "Game.Systems",
            additionalReferences: new[] { rootReference, featureReference });

        AssertNoErrors(system.Diagnostics.Concat(system.Compilation.GetDiagnostics()));
    }

    [Fact]
    public void FeatureOwnedComponentsInOneAssemblySharePartialMatcherClass()
    {
        var root = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(RootSource, "Game.Root");
        AssertNoErrors(root.Diagnostics.Concat(root.Compilation.GetDiagnostics()));
        var rootReference = CodeGenerationTestHelper.CreateReferenceFromCompilation(root.Compilation);

        const string featureSource = @"
using Entitas;
using Entitas.CodeGeneration.Attributes;
using Game.Root;

[assembly: EntitasAssembly(""Player"")]

namespace Game.Feature
{
    [Main]
    public sealed class PlayerComponent : IComponent { }

    [Main]
    public sealed class PlayerBuffComponent : IComponent { }
}
";

        var feature = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(
            featureSource,
            "Game.Feature",
            additionalReferences: new[] { rootReference });

        AssertNoErrors(feature.Diagnostics.Concat(feature.Compilation.GetDiagnostics()));
        var matcherFiles = GetGeneratedFileNames(feature.Result)
            .Where(fileName => fileName.Contains("MainPlayerMatcher", StringComparison.Ordinal))
            .ToArray();

        matcherFiles.Should().BeEquivalentTo(new[]
        {
            "Game.Feature.MainPlayerMatcher.g.cs",
            "Game.Feature.MainPlayerMatcher.Player.g.cs",
            "Game.Feature.MainPlayerMatcher.PlayerBuff.g.cs"
        });

        var matcherDeclarationSource = GetGeneratedSource(feature.Result, "Game.Feature.MainPlayerMatcher.g.cs");
        matcherDeclarationSource.Should().Contain("public static partial class MainPlayerMatcher");

        var playerMatcherSource = GetGeneratedSource(feature.Result, "Game.Feature.MainPlayerMatcher.Player.g.cs");
        playerMatcherSource.Should().Contain("namespace Game.Feature");
        playerMatcherSource.Should().Contain("public static partial class MainPlayerMatcher");
        playerMatcherSource.Should().Contain("public static global::Entitas.IMatcher<MainEntity> Player()");
        playerMatcherSource.Should().NotContain("PlayerBuff()");

        var playerBuffMatcherSource = GetGeneratedSource(feature.Result, "Game.Feature.MainPlayerMatcher.PlayerBuff.g.cs");
        playerBuffMatcherSource.Should().Contain("namespace Game.Feature");
        playerBuffMatcherSource.Should().Contain("public static partial class MainPlayerMatcher");
        playerBuffMatcherSource.Should().Contain("public static global::Entitas.IMatcher<MainEntity> PlayerBuff()");
        playerBuffMatcherSource.Should().NotContain("Player()");
    }

    [Fact]
    public void FeatureOwnedMatcherPartialHintNamesKeepBaseMatcherAndUseShortComponentName()
    {
        const string rootSource = @"
namespace Sample.MultiAssembly.Root
{
    public sealed class SharedAttribute : Entitas.CodeGeneration.Attributes.ContextAttribute
    {
        public SharedAttribute() : base(""Shared"") { }
    }
}
";

        const string featureSource = @"
using Entitas;
using Entitas.CodeGeneration.Attributes;
using Sample.MultiAssembly.Root;

[assembly: EntitasAssembly(""Player"")]

namespace Sample.MultiAssembly.FeatureA
{
    [Shared]
    public sealed class LevelComponent : IComponent
    {
        public int Value;
    }
}
";

        var root = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(rootSource, "Sample.MultiAssembly.Root");
        AssertNoErrors(root.Diagnostics.Concat(root.Compilation.GetDiagnostics()));
        var rootReference = CodeGenerationTestHelper.CreateReferenceFromCompilation(root.Compilation);

        var feature = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(
            featureSource,
            "Sample.MultiAssembly.FeatureA",
            additionalReferences: new[] { rootReference });

        AssertNoErrors(feature.Diagnostics.Concat(feature.Compilation.GetDiagnostics()));
        var generatedFileNames = GetGeneratedFileNames(feature.Result);

        generatedFileNames.Should().Contain("Sample.MultiAssembly.FeatureA.SharedPlayerMatcher.g.cs");
        generatedFileNames.Should().Contain("Sample.MultiAssembly.FeatureA.SharedPlayerMatcher.Level.g.cs");
        generatedFileNames.Should().NotContain("Sample.MultiAssembly.FeatureA.SharedPlayerMatcher.SampleMultiAssemblyFeatureALevel.g.cs");

        GetGeneratedSource(feature.Result, "Sample.MultiAssembly.FeatureA.SharedPlayerMatcher.g.cs")
            .Should().Contain("public static partial class SharedPlayerMatcher");
        GetGeneratedSource(feature.Result, "Sample.MultiAssembly.FeatureA.SharedPlayerMatcher.Level.g.cs")
            .Should().Contain("public static global::Entitas.IMatcher<SharedEntity> Level()");
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
        GetGeneratedFileNames(feature.Result).Should().Contain(fileName =>
            fileName.EndsWith("MainGameFeatureFallbackMatcher.g.cs", StringComparison.Ordinal));
        GetGeneratedSource(feature.Result, "MainGameFeatureFallbackAssemblySchemaExtensions.g.cs")
            .Should().Contain("AddGameFeatureFallbackAssembly(this global::Entitas.ContextSchemaBuilder builder)");
        GetGeneratedSourceBySuffix(feature.Result, "MainGameFeatureFallbackMatcher.g.cs")
            .Should().Contain("public static partial class MainGameFeatureFallbackMatcher");
    }

    [Fact]
    public void AssemblyCSharpCanContributeFeatureOwnedComponentsToAnAsmdefRootContext()
    {
        var root = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(RootSource, "Game.Root");
        AssertNoErrors(root.Diagnostics.Concat(root.Compilation.GetDiagnostics()));
        var rootReference = CodeGenerationTestHelper.CreateReferenceFromCompilation(root.Compilation);

        const string assemblyCSharpSource = @"
using Entitas;
using Entitas.CodeGeneration.Attributes;
using Game.Root;

[Main]
[Event(EventTarget.Any)]
public sealed class ManaComponent : IComponent
{
    [EntityIndex]
    public int Value;
}

[Main, Unique]
public sealed class SessionComponent : IComponent
{
    [PrimaryEntityIndex]
    public string Id;
}

[Main]
[Cleanup(CleanupMode.RemoveComponent)]
public sealed class ExpiredComponent : IComponent { }

public static class AssemblyCSharpBootstrap
{
    public static ContextSchema CreateSchema()
    {
        var builder = MainContext.CreateSchemaBuilder();
        builder = MainAssemblyCSharpAssemblySchemaExtensions.AddAssemblyCSharpAssembly(builder);
        return builder.Build();
    }

    public static void UseGeneratedApis(MainContext context)
    {
        var listener = context.CreateEntity();
        listener.AddAnyManaListener(new ManaListener());

        var entity = context.SetSession(""play-mode"");
        entity.AddMana(3);
        entity.SetExpired(true);

        _ = MainAssemblyCSharpMatcher.Mana();
        _ = context.GetEntityWithSessionId(""play-mode"");
        _ = context.GetEntitiesWithManaValue(3);
    }
}

public sealed class ManaListener : IAnyManaListener
{
    public void OnAnyMana(MainEntity entity, int value) { }
}
";

        var feature = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(
            assemblyCSharpSource,
            "Assembly-CSharp",
            additionalReferences: new[] { rootReference });

        AssertNoErrors(feature.Diagnostics.Concat(feature.Compilation.GetDiagnostics()));

        var generatedFileNames = GetGeneratedFileNames(feature.Result);
        generatedFileNames.Should().Contain("MainAssemblyCSharpAssemblySchemaExtensions.g.cs");
        generatedFileNames.Should().Contain("MainAssemblyCSharpMatcher.g.cs");
        generatedFileNames.Should().Contain("MainAssemblyCSharpMatcher.Mana.g.cs");
        generatedFileNames.Should().Contain("MainAssemblyCSharpMatcher.Session.g.cs");

        var assemblySchemaSource = GetGeneratedSource(feature.Result, "MainAssemblyCSharpAssemblySchemaExtensions.g.cs");
        assemblySchemaSource.Should().Contain("AddAssemblyCSharpAssembly(this global::Entitas.ContextSchemaBuilder builder)");
        assemblySchemaSource.Should().Contain("builder = MainManaComponentSchemaExtensions.AddMainMana(builder);");
        assemblySchemaSource.Should().Contain("builder = MainManaEntityIndicesSchemaExtensions.AddMainManaEntityIndices(builder);");
        assemblySchemaSource.Should().Contain("builder = AnyManaEventSystemSchemaExtensions.AddAnyManaEventSystem(builder);");
        assemblySchemaSource.Should().Contain("builder = MainSessionEntityIndicesSchemaExtensions.AddMainSessionEntityIndices(builder);");
        assemblySchemaSource.Should().Contain("builder = RemoveExpiredMainSystemSchemaExtensions.AddRemoveExpiredMainSystem(builder);");

        GetGeneratedSource(feature.Result, "MainAssemblyCSharpMatcher.Mana.g.cs")
            .Should().Contain("public static global::Entitas.IMatcher<MainEntity> Mana()")
            .And.Contain("global::Entitas.Matcher<MainEntity>.AllOf(MainManaComponentHandle.Handle)");
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

    [Fact]
    public void FeatureOwnedMatcherGenerationCoversMultiContextComponents()
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
    [Main, Meta]
    public sealed class SharedStateComponent : IComponent { }
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
        var generatedFileNames = GetGeneratedFileNames(feature.Result);

        generatedFileNames.Should().Contain("Game.Feature.MainPlayerMatcher.g.cs");
        generatedFileNames.Should().Contain("Game.Feature.MainPlayerMatcher.SharedState.g.cs");
        generatedFileNames.Should().Contain("Game.Feature.MetaPlayerMatcher.g.cs");
        generatedFileNames.Should().Contain("Game.Feature.MetaPlayerMatcher.SharedState.g.cs");

        GetGeneratedSource(feature.Result, "Game.Feature.MainPlayerMatcher.SharedState.g.cs")
            .Should().Contain("public static global::Entitas.IMatcher<MainEntity> SharedState()")
            .And.Contain("global::Entitas.Matcher<MainEntity>.AllOf(global::Game.Feature.MainSharedStateComponentHandle.Handle)");

        GetGeneratedSource(feature.Result, "Game.Feature.MetaPlayerMatcher.SharedState.g.cs")
            .Should().Contain("public static global::Entitas.IMatcher<MetaEntity> SharedState()")
            .And.Contain("global::Entitas.Matcher<MetaEntity>.AllOf(global::Game.Feature.MetaSharedStateComponentHandle.Handle)");
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
