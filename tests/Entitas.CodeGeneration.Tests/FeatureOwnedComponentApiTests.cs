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
            fileName.EndsWith("MainUserMatcherExtensions.g.cs", StringComparison.Ordinal));
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

        var matcherSource = GetGeneratedSourceBySuffix(feature.Result, "MainUserMatcherExtensions.g.cs");
        matcherSource.Should().Contain("public static class MainUserMatcherExtensions");
        matcherSource.Should().Contain("public static global::Entitas.IMatcher<MainEntity> User(this MainMatcher matcher)");
        matcherSource.Should().Contain("global::Entitas.Matcher<MainEntity>.AllOf(global::Game.Feature.MainUserComponentHandle.Handle)");
        matcherSource.Should().NotContain("MainComponentsLookup");
        matcherSource.Should().NotContain("MainPlayerMatcher");

        var loadingMatcherSource = GetGeneratedSourceBySuffix(feature.Result, "MainLoadingMatcherExtensions.g.cs");
        loadingMatcherSource.Should().Contain("public static class MainLoadingMatcherExtensions");
        loadingMatcherSource.Should().Contain("public static global::Entitas.IMatcher<MainEntity> Loading(this MainMatcher matcher)");
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

        var assemblySchemaSource = GetGeneratedSource(feature.Result, "MainGameFeatureSchemaExtensions.g.cs");
        assemblySchemaSource.Should().Contain("public static global::Entitas.ContextSchemaBuilder AddGameFeatureSchema(this global::Entitas.ContextSchemaBuilder builder)");
        assemblySchemaSource.Should().Contain("builder = global::Game.Feature.MainUserComponentSchemaExtensions.AddMainUser(builder);");
        assemblySchemaSource.Should().Contain("builder = global::Game.Feature.MainUserEntityIndicesSchemaExtensions.AddMainUserEntityIndices(builder);");
        assemblySchemaSource.Should().Contain("builder = global::Game.Feature.MainReactiveComponentSchemaExtensions.AddMainReactive(builder);");
        assemblySchemaSource.Should().Contain("builder = global::Game.Feature.GameFeatureAnyReactiveEventSystemSchemaExtensions.AddGameFeatureAnyReactiveEventSystem(builder);");
        assemblySchemaSource.Should().Contain("builder = global::Game.Feature.RemoveGameFeatureCleanupMeMainSystemSchemaExtensions.AddRemoveGameFeatureCleanupMeMainSystem(builder);");
    }

    [Fact]
    public void FeatureAssemblySchemaRegistrationComposesEventAndCleanupSystems()
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

        const string bootstrapSource = @"
using Entitas;
using Game.Feature;

public sealed class BootstrapSystems : Systems
{
    public BootstrapSystems(Contexts contexts)
    {
        var schema = MainContext.CreateSchemaBuilder()
            .AddGameFeatureSchema()
            .Build();

        contexts.RegisterMain(schema);
        schema.InitializeEntityIndices(contexts);

        Add(schema.CreateEventSystems(contexts));
        Add(schema.CreateCleanupSystems(contexts));
    }
}
";

        var bootstrap = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(
            bootstrapSource,
            "Game.Bootstrap",
            additionalReferences: new[] { rootReference, featureReference });

        AssertNoErrors(bootstrap.Diagnostics.Concat(bootstrap.Compilation.GetDiagnostics()));
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
        return context.CreateCollector(MainMatcher.Instance.User().Added());
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
    public void FeatureOwnedComponentsGenerateStableMatcherExtensions()
    {
        var root = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(RootSource, "Game.Root");
        AssertNoErrors(root.Diagnostics.Concat(root.Compilation.GetDiagnostics()));
        var rootReference = CodeGenerationTestHelper.CreateReferenceFromCompilation(root.Compilation);

        const string featureSource = @"
using Entitas;
using Entitas.CodeGeneration.Attributes;
using Game.Root;

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
            .Where(fileName => fileName.Contains("MatcherExtensions", StringComparison.Ordinal))
            .ToArray();

        matcherFiles.Should().BeEquivalentTo(new[]
        {
            "Game.Feature.MainPlayerMatcherExtensions.g.cs",
            "Game.Feature.MainPlayerBuffMatcherExtensions.g.cs"
        });

        var playerMatcherSource = GetGeneratedSource(feature.Result, "Game.Feature.MainPlayerMatcherExtensions.g.cs");
        playerMatcherSource.Should().Contain("namespace Game.Feature");
        playerMatcherSource.Should().Contain("public static class MainPlayerMatcherExtensions");
        playerMatcherSource.Should().Contain("public static global::Entitas.IMatcher<MainEntity> Player(this MainMatcher matcher)");
        playerMatcherSource.Should().NotContain("PlayerBuff(");

        var playerBuffMatcherSource = GetGeneratedSource(feature.Result, "Game.Feature.MainPlayerBuffMatcherExtensions.g.cs");
        playerBuffMatcherSource.Should().Contain("namespace Game.Feature");
        playerBuffMatcherSource.Should().Contain("public static class MainPlayerBuffMatcherExtensions");
        playerBuffMatcherSource.Should().Contain("public static global::Entitas.IMatcher<MainEntity> PlayerBuff(this MainMatcher matcher)");
        playerBuffMatcherSource.Should().NotContain("Player(");
    }

    [Fact]
    public void FeatureOwnedMatcherExtensionHintNamesUseContextAndShortComponentName()
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

        generatedFileNames.Should().Contain("Sample.MultiAssembly.FeatureA.SharedLevelMatcherExtensions.g.cs");
        generatedFileNames.Should().NotContain("Sample.MultiAssembly.FeatureA.SharedPlayerMatcher.g.cs");
        generatedFileNames.Should().NotContain("Sample.MultiAssembly.FeatureA.SharedPlayerMatcher.Level.g.cs");

        GetGeneratedSource(feature.Result, "Sample.MultiAssembly.FeatureA.SharedLevelMatcherExtensions.g.cs")
            .Should().Contain("public static global::Entitas.IMatcher<SharedEntity> Level(this SharedMatcher matcher)");
    }

    [Fact]
    public void UsesSanitizedAssemblyNameForSchemaRegistration()
    {
        var root = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(RootSource, "Game.Root");
        AssertNoErrors(root.Diagnostics.Concat(root.Compilation.GetDiagnostics()));
        var rootReference = CodeGenerationTestHelper.CreateReferenceFromCompilation(root.Compilation);

        var feature = CodeGenerationTestHelper.RunGeneratorAndUpdateCompilation(
            FeatureSource,
            "Game.Feature-Fallback",
            additionalReferences: new[] { rootReference });

        AssertNoErrors(feature.Diagnostics.Concat(feature.Compilation.GetDiagnostics()));
        GetGeneratedFileNames(feature.Result).Should().Contain("MainGameFeatureFallbackSchemaExtensions.g.cs");
        GetGeneratedFileNames(feature.Result).Should().NotContain(fileName =>
            fileName.EndsWith("MainGameFeatureFallbackMatcher.g.cs", StringComparison.Ordinal));
        GetGeneratedSource(feature.Result, "MainGameFeatureFallbackSchemaExtensions.g.cs")
            .Should().Contain("AddGameFeatureFallbackSchema(this global::Entitas.ContextSchemaBuilder builder)");
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
        builder = MainAssemblyCSharpSchemaExtensions.AddAssemblyCSharpSchema(builder);
        return builder.Build();
    }

    public static void UseGeneratedApis(MainContext context)
    {
        var listener = context.CreateEntity();
        listener.AddAnyManaListener(new ManaListener());

        var entity = context.SetSession(""play-mode"");
        entity.AddMana(3);
        entity.SetExpired(true);

        _ = context.Matcher.Mana();
        _ = context.GetEntityWithSession(""play-mode"");
        _ = context.GetEntitiesWithMana(3);
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
        generatedFileNames.Should().Contain("MainAssemblyCSharpSchemaExtensions.g.cs");
        generatedFileNames.Should().Contain("MainManaMatcherExtensions.g.cs");
        generatedFileNames.Should().Contain("MainSessionMatcherExtensions.g.cs");

        var assemblySchemaSource = GetGeneratedSource(feature.Result, "MainAssemblyCSharpSchemaExtensions.g.cs");
        assemblySchemaSource.Should().Contain("AddAssemblyCSharpSchema(this global::Entitas.ContextSchemaBuilder builder)");
        assemblySchemaSource.Should().Contain("builder = MainManaComponentSchemaExtensions.AddMainMana(builder);");
        assemblySchemaSource.Should().Contain("builder = MainManaEntityIndicesSchemaExtensions.AddMainManaEntityIndices(builder);");
        assemblySchemaSource.Should().Contain("builder = AnyManaEventSystemSchemaExtensions.AddAnyManaEventSystem(builder);");
        assemblySchemaSource.Should().Contain("builder = MainSessionEntityIndicesSchemaExtensions.AddMainSessionEntityIndices(builder);");
        assemblySchemaSource.Should().Contain("builder = RemoveExpiredMainSystemSchemaExtensions.AddRemoveExpiredMainSystem(builder);");

        GetGeneratedSource(feature.Result, "MainManaMatcherExtensions.g.cs")
            .Should().Contain("public static global::Entitas.IMatcher<MainEntity> Mana(this MainMatcher matcher)")
            .And.Contain("global::Entitas.Matcher<MainEntity>.AllOf(MainManaComponentHandle.Handle)");

        var sessionIndexSource = GetGeneratedSourceBySuffix(feature.Result, "SessionEntityIndices.g.cs");
        sessionIndexSource.Should().Contain("GetEntityWithSession(this MainContext context, string Id)");
        sessionIndexSource.Should().NotContain("GetEntityWithSessionId");

        var manaIndexSource = GetGeneratedSourceBySuffix(feature.Result, "ManaEntityIndices.g.cs");
        manaIndexSource.Should().Contain("GetEntitiesWithMana(this MainContext context, int Value)");
        manaIndexSource.Should().NotContain("GetEntitiesWithManaValue");
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
        GetGeneratedSource(feature.Result, "MainGameFeatureSchemaExtensions.g.cs")
            .Should().Contain("AddMainGameFeatureSchema(this global::Entitas.ContextSchemaBuilder builder)");
        GetGeneratedSource(feature.Result, "MetaGameFeatureSchemaExtensions.g.cs")
            .Should().Contain("AddMetaGameFeatureSchema(this global::Entitas.ContextSchemaBuilder builder)");
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

        generatedFileNames.Should().Contain("Game.Feature.MainSharedStateMatcherExtensions.g.cs");
        generatedFileNames.Should().Contain("Game.Feature.MetaSharedStateMatcherExtensions.g.cs");

        GetGeneratedSource(feature.Result, "Game.Feature.MainSharedStateMatcherExtensions.g.cs")
            .Should().Contain("public static global::Entitas.IMatcher<MainEntity> SharedState(this MainMatcher matcher)")
            .And.Contain("global::Entitas.Matcher<MainEntity>.AllOf(global::Game.Feature.MainSharedStateComponentHandle.Handle)");

        GetGeneratedSource(feature.Result, "Game.Feature.MetaSharedStateMatcherExtensions.g.cs")
            .Should().Contain("public static global::Entitas.IMatcher<MetaEntity> SharedState(this MetaMatcher matcher)")
            .And.Contain("global::Entitas.Matcher<MetaEntity>.AllOf(global::Game.Feature.MetaSharedStateComponentHandle.Handle)");
    }

    [Fact]
    public void FeatureComponentWithNoContextAttributeFallsBackToGameContext()
    {
        const string rootSource = @"
namespace Game.Root
{
    public sealed class GameAttribute : Entitas.CodeGeneration.Attributes.ContextAttribute
    {
        public GameAttribute() : base(""Game"") { }
    }
}
";

        const string featureSource = @"
using Entitas;
using Entitas.CodeGeneration.Attributes;

public sealed class ScoreComponent : IComponent
{
    public int Value;
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
        generatedFileNames.Should().Contain("GameGameFeatureSchemaExtensions.g.cs");
        generatedFileNames.Should().Contain(fileName =>
            fileName.EndsWith("GameScoreMatcherExtensions.g.cs", StringComparison.Ordinal));

        var scoreSource = GetGeneratedSourceBySuffix(feature.Result, "ScoreComponent.g.cs");
        scoreSource.Should().Contain("GameScoreComponentHandle");
        scoreSource.Should().Contain("AddScore(this GameEntity");

        var assemblySchemaSource = GetGeneratedSource(feature.Result, "GameGameFeatureSchemaExtensions.g.cs");
        assemblySchemaSource.Should().Contain("AddGameFeatureSchema");
        assemblySchemaSource.Should().Contain("AddGameScore");
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
