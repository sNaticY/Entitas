using System.Collections.Immutable;
using Entitas.CodeGeneration.Cleanup;
using Entitas.CodeGeneration.Components;
using Entitas.CodeGeneration.Components.Data;
using Entitas.CodeGeneration.ComponentsLookups;
using Entitas.CodeGeneration.Contexts;
using Entitas.CodeGeneration.Contexts.Data;
using Entitas.CodeGeneration.EntityIndex;
using Entitas.CodeGeneration.Events;
using Entitas.CodeGeneration.Features;
using Entitas.CodeGeneration.VisualDebugging;
using Microsoft.CodeAnalysis;

namespace Entitas.CodeGeneration;

[Generator]
public class EntitasGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilationAndOptions = context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider);

        var generatorOptions = compilationAndOptions
            .Select(static (input, _) => EntitasGeneratorOptions.From(input.Right, input.Left));

        var shouldRun = compilationAndOptions
            .Select(static (input, _) => EntitasGeneratorOptions.From(input.Right, input.Left).ShouldRun(input.Left.AssemblyName));

        var contextsData = ContextGenerationHelper.GetContextsData(context);
        RegisterContextRootGeneration(context, shouldRun, generatorOptions, contextsData);

        var componentsData = ComponentGenerationHelper.GetComponentsData(context, generatorOptions);
        var componentsByContextNameLookup = ComponentsLookupGenerationHelper.GetComponentsByContextNameLookup(componentsData);
        RegisterComponentOwnedSourcesGeneration(context, shouldRun, generatorOptions, contextsData, componentsByContextNameLookup);
        RegisterContextSharedSourcesGeneration(context, shouldRun, generatorOptions, contextsData, componentsData, componentsByContextNameLookup);

        RegisterVisualDebuggingGeneration(context, generatorOptions, contextsData);
    }

    void RegisterContextRootGeneration(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<bool> shouldRun,
        IncrementalValueProvider<EntitasGeneratorOptions> generatorOptions,
        in IncrementalValueProvider<ImmutableArray<ContextData>> contextsData)
    {
        var combinedInput = shouldRun
            .Combine(generatorOptions)
            .Combine(contextsData)
            .Select(static (input, _) => new ContextRootSourceInput(input.Left.Left, input.Left.Right, input.Right));

        // This will be triggered for any context change (ContextAttribute class)
        context.RegisterSourceOutput(combinedInput,
            static (spc, source) => GenerateContextRootSources(source, spc));
    }

    static void GenerateContextRootSources(ContextRootSourceInput input, SourceProductionContext spc)
    {
        if (!input.ShouldRun)
            return;

        var options = input.Options;
        var contextsData = input.ContextsData;
        if (contextsData.IsDefaultOrEmpty)
            return;

        ContextGenerationHelper.GenerateContexts(spc, contextsData, options);
    }

    void RegisterContextSharedSourcesGeneration(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<bool> shouldRun,
        IncrementalValueProvider<EntitasGeneratorOptions> generatorOptions,
        in IncrementalValueProvider<ImmutableArray<ContextData>> contextsData,
        in IncrementalValueProvider<ImmutableArray<ComponentData>> componentsData,
        in IncrementalValueProvider<ImmutableDictionary<string, ImmutableArray<ComponentData>>> componentsByContextNameLookup)
    {
        var contextSharedData = contextsData
            .Combine(componentsData)
            .Combine(componentsByContextNameLookup)
            .Select(static (input, _) => new ContextSharedData(input.Left.Left, input.Left.Right, input.Right));

        var combinedInput = shouldRun
            .Combine(generatorOptions)
            .Combine(contextSharedData)
            .Select(static (input, _) => new ContextSharedSourceInput(input.Left.Left, input.Left.Right, input.Right));

        // Warning: This will be triggered for ANY context or component change
        // Keep it as light as possible
        context.RegisterSourceOutput(combinedInput,
            static (spc, source) => GenerateContextSharedSources(source, spc));
    }

    static void GenerateContextSharedSources(ContextSharedSourceInput input, SourceProductionContext spc)
    {
        if (!input.ShouldRun)
            return;

        var options = input.Options;
        var contextsData = input.Data.ContextsData;
        var contextLookup = contextsData.ToDictionary(ctx => ctx.ContextName);

        var componentsByContextNameLookup = input.Data.ComponentsByContextNameLookup;

        if (options.ComponentsLookupGenerationEnabled)
            ComponentsLookupGenerationHelper.GenerateComponentsLookups(spc, componentsByContextNameLookup, contextLookup, options);

        if (options.ComponentEntityIndexGenerationEnabled)
            EntityIndexGenerationHelper.GenerateEntityIndices(spc, componentsByContextNameLookup, contextLookup);

        if (options.CleanupGenerationEnabled)
            CleanupGenerationHelper.GenerateCleanupSystems(spc, componentsByContextNameLookup, contextLookup);

        if (options.ComponentEventSystemsGenerationEnabled)
            EventsGenerationHelper.GenerateEventSystems(spc, componentsByContextNameLookup, contextLookup);

        FeatureSchemaRegistrationGenerationHelper.GenerateFeatureSchemaRegistrations(spc, componentsByContextNameLookup, contextLookup, options);
        ContextRegistrationGenerationHelper.GenerateContextRegistrations(spc, contextsData, componentsByContextNameLookup, options);
    }

    void RegisterComponentOwnedSourcesGeneration(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<bool> shouldRun,
        IncrementalValueProvider<EntitasGeneratorOptions> generatorOptions,
        in IncrementalValueProvider<ImmutableArray<ContextData>> contextsData,
        in IncrementalValueProvider<ImmutableDictionary<string, ImmutableArray<ComponentData>>> componentsByContextNameLookup)
    {
        // Extract all component-owned sources. Context-root assemblies keep the full
        // single-assembly surface; feature assemblies emit only handle-based plain APIs.
        var componentByContext = contextsData
            .Combine(componentsByContextNameLookup)
            .SelectMany((pair, _) =>
            {
                var (contexts, componentLookup) = pair;
                var contextLookup = contexts.ToDictionary(contextData => contextData.ContextName);

                return componentLookup.SelectMany(contextComponentsPair =>
                {
                    var contextName = contextComponentsPair.Key;
                    var contextRootIsLocal = contextLookup.TryGetValue(contextName, out var contextData);
                    if (!contextRootIsLocal)
                        contextData = new ContextData(contextName);

                    return contextComponentsPair.Value
                        .Where(component => contextRootIsLocal || ShouldGenerateFeatureOwnedPlainApis(component))
                        .Select(component => new ComponentByContextSource(contextData, component, contextRootIsLocal));
                });
            });

        var conditionalComponentByContext = componentByContext
            .Combine(generatorOptions)
            .Combine(shouldRun)
            .Select(static (pair, _) => new ComponentOwnedSourceInput(pair.Right, pair.Left.Right, pair.Left.Left));

        // This is triggered for:
        // - individual components that have changed, or
        // - all components belonging to a context that has changed.
        context.RegisterSourceOutput(conditionalComponentByContext,
            static (spc, source) => GenerateComponentOwnedSources(source, spc));

        var featureOwnedMatcherByContext = contextsData
            .Combine(componentsByContextNameLookup)
            .SelectMany((pair, _) =>
            {
                var (contexts, componentLookup) = pair;
                var contextLookup = contexts.ToDictionary(contextData => contextData.ContextName);

                return componentLookup
                    .Where(contextComponentsPair => !contextLookup.ContainsKey(contextComponentsPair.Key))
                    .Select(contextComponentsPair => new FeatureOwnedMatcherGroup(
                        new ContextData(contextComponentsPair.Key),
                        contextComponentsPair.Value
                            .Where(component => ShouldGenerateFeatureOwnedPlainApis(component))
                            .ToImmutableArray()))
                    .Where(group => group.ComponentsData.Length > 0);
            });

        var conditionalFeatureOwnedMatcherByContext = featureOwnedMatcherByContext
            .Combine(generatorOptions)
            .Combine(shouldRun)
            .Select(static (pair, _) => new FeatureOwnedMatcherSourceInput(pair.Right, pair.Left.Right, pair.Left.Left));

        context.RegisterSourceOutput(conditionalFeatureOwnedMatcherByContext,
            static (spc, source) => GenerateFeatureOwnedMatcherSources(source, spc));
    }

    static bool ShouldGenerateFeatureOwnedPlainApis(in ComponentData componentData) =>
        componentData.HasExplicitContexts;

    static void GenerateComponentOwnedSources(ComponentOwnedSourceInput input, SourceProductionContext spc)
    {
        if (!input.ShouldRun)
            return;

        var componentData = input.ComponentByContext.ComponentData;
        var contextData = input.ComponentByContext.ContextData;

        if (!input.ComponentByContext.ContextRootIsLocal)
        {
            ComponentGenerationHelper.GeneratePlainComponentApis(spc, componentData, input.Options, contextData);
            if (!componentData.IsGenerated)
            {
                GeneratePerComponentEventAndCleanupSources(spc, componentData, input.Options, contextData);

                if (input.Options.ComponentEntityIndexGenerationEnabled)
                    EntityIndexGenerationHelper.GenerateComponentEntityIndexRegistrations(spc, componentData, contextData);
            }

            return;
        }

        GenerateSlotDependentComponentSources(spc, componentData, input.Options, contextData);

        if (componentData.IsGenerated)
            return;

        GeneratePerComponentEventAndCleanupSources(spc, componentData, input.Options, contextData);
    }

    static void GenerateFeatureOwnedMatcherSources(FeatureOwnedMatcherSourceInput input, SourceProductionContext spc)
    {
        if (!input.ShouldRun)
            return;

        ComponentGenerationHelper.GenerateFeatureOwnedComponentMatcherApis(
            spc,
            input.MatcherGroup.ComponentsData,
            input.Options,
            input.MatcherGroup.ContextData);
    }

    static void GenerateSlotDependentComponentSources(
        SourceProductionContext spc,
        in ComponentData componentData,
        in EntitasGeneratorOptions options,
        in ContextData contextData)
    {
        ComponentGenerationHelper.GeneratePlainComponentApis(spc, componentData, options, contextData);
        ComponentGenerationHelper.GenerateComponentMatcherApi(spc, componentData, options, contextData);
    }

    static void GeneratePerComponentEventAndCleanupSources(
        SourceProductionContext spc,
        in ComponentData componentData,
        in EntitasGeneratorOptions options,
        in ContextData contextData)
    {
        if (options.ComponentEventsGenerationEnabled)
            EventsGenerationHelper.GenerateComponentEvents(spc, componentData, contextData);

        if (options.CleanupGenerationEnabled && componentData.HasCleanupAttribute)
            CleanupGenerationHelper.GenerateComponentCleanupSystem(spc, componentData, contextData);
    }


    void RegisterVisualDebuggingGeneration(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<EntitasGeneratorOptions> generatorOptions,
        in IncrementalValueProvider<ImmutableArray<ContextData>> contextsData)
    {
        var shouldRun = context.CompilationProvider.Combine(generatorOptions)
            .Select(static (input, _) => input.Right.ShouldGenerateVisualDebugging(input.Left.AssemblyName));

        var combinedInput = shouldRun
            .Combine(contextsData)
            .Select(static (input, _) => new VisualDebuggingSourceInput(input.Left, input.Right));
        context.RegisterSourceOutput(combinedInput,
            static (spc, source) => GenerateVisualDebugging(source, spc));
    }

    static void GenerateVisualDebugging(VisualDebuggingSourceInput input, SourceProductionContext spc)
    {
        if (!input.ShouldRun)
            return;

        var contextsData = input.ContextsData;
        if (contextsData.IsDefaultOrEmpty)
            return;

        // Context Observers, Feature
        VisualDebuggingGenerationHelper.Generate(spc, contextsData);
    }

    readonly struct ContextRootSourceInput
    {
        public readonly bool ShouldRun;
        public readonly EntitasGeneratorOptions Options;
        public readonly ImmutableArray<ContextData> ContextsData;

        public ContextRootSourceInput(
            bool shouldRun,
            EntitasGeneratorOptions options,
            ImmutableArray<ContextData> contextsData)
        {
            ShouldRun = shouldRun;
            Options = options;
            ContextsData = contextsData;
        }
    }

    readonly struct ContextSharedData
    {
        public readonly ImmutableArray<ContextData> ContextsData;
        public readonly ImmutableArray<ComponentData> ComponentsData;
        public readonly ImmutableDictionary<string, ImmutableArray<ComponentData>> ComponentsByContextNameLookup;

        public ContextSharedData(
            ImmutableArray<ContextData> contextsData,
            ImmutableArray<ComponentData> componentsData,
            ImmutableDictionary<string, ImmutableArray<ComponentData>> componentsByContextNameLookup)
        {
            ContextsData = contextsData;
            ComponentsData = componentsData;
            ComponentsByContextNameLookup = componentsByContextNameLookup;
        }
    }

    readonly struct ContextSharedSourceInput
    {
        public readonly bool ShouldRun;
        public readonly EntitasGeneratorOptions Options;
        public readonly ContextSharedData Data;

        public ContextSharedSourceInput(bool shouldRun, EntitasGeneratorOptions options, ContextSharedData data)
        {
            ShouldRun = shouldRun;
            Options = options;
            Data = data;
        }
    }

    readonly struct ComponentByContextSource
    {
        public readonly ContextData ContextData;
        public readonly ComponentData ComponentData;
        public readonly bool ContextRootIsLocal;

        public ComponentByContextSource(
            ContextData contextData,
            ComponentData componentData,
            bool contextRootIsLocal)
        {
            ContextData = contextData;
            ComponentData = componentData;
            ContextRootIsLocal = contextRootIsLocal;
        }
    }

    readonly struct ComponentOwnedSourceInput
    {
        public readonly bool ShouldRun;
        public readonly EntitasGeneratorOptions Options;
        public readonly ComponentByContextSource ComponentByContext;

        public ComponentOwnedSourceInput(
            bool shouldRun,
            EntitasGeneratorOptions options,
            ComponentByContextSource componentByContext)
        {
            ShouldRun = shouldRun;
            Options = options;
            ComponentByContext = componentByContext;
        }
    }

    readonly struct FeatureOwnedMatcherGroup
    {
        public readonly ContextData ContextData;
        public readonly ImmutableArray<ComponentData> ComponentsData;

        public FeatureOwnedMatcherGroup(ContextData contextData, ImmutableArray<ComponentData> componentsData)
        {
            ContextData = contextData;
            ComponentsData = componentsData;
        }
    }

    readonly struct FeatureOwnedMatcherSourceInput
    {
        public readonly bool ShouldRun;
        public readonly EntitasGeneratorOptions Options;
        public readonly FeatureOwnedMatcherGroup MatcherGroup;

        public FeatureOwnedMatcherSourceInput(
            bool shouldRun,
            EntitasGeneratorOptions options,
            FeatureOwnedMatcherGroup matcherGroup)
        {
            ShouldRun = shouldRun;
            Options = options;
            MatcherGroup = matcherGroup;
        }
    }

    readonly struct VisualDebuggingSourceInput
    {
        public readonly bool ShouldRun;
        public readonly ImmutableArray<ContextData> ContextsData;

        public VisualDebuggingSourceInput(bool shouldRun, ImmutableArray<ContextData> contextsData)
        {
            ShouldRun = shouldRun;
            ContextsData = contextsData;
        }
    }
}
