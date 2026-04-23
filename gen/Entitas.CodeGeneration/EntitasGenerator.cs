using System.Collections.Immutable;
using Entitas.CodeGeneration.Cleanup;
using Entitas.CodeGeneration.Components;
using Entitas.CodeGeneration.Components.Data;
using Entitas.CodeGeneration.ComponentsLookups;
using Entitas.CodeGeneration.Contexts;
using Entitas.CodeGeneration.Contexts.Data;
using Entitas.CodeGeneration.EntityIndex;
using Entitas.CodeGeneration.Events;
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
        var combinedInput = shouldRun.Combine(generatorOptions.Combine(contextsData));

        // This will be triggered for any context change (ContextAttribute class)
        context.RegisterSourceOutput(combinedInput,
            static (spc, source) => GenerateContextRootSources(source, spc));
    }

    static void GenerateContextRootSources(
        (bool, (EntitasGeneratorOptions, ImmutableArray<ContextData>)) input,
        SourceProductionContext spc)
    {
        if (!input.Item1) // check shouldRun
            return;

        var options = input.Item2.Item1;
        var contextsData = input.Item2.Item2;
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
        var contextsAndComponents = contextsData.Combine(componentsData);
        var contextsAndComponentsWithLookup = contextsAndComponents.Combine(componentsByContextNameLookup);
        var combinedInput = shouldRun.Combine(generatorOptions.Combine(contextsAndComponentsWithLookup));

        // Warning: This will be triggered for ANY context or component change
        // Keep it as light as possible
        context.RegisterSourceOutput(combinedInput,
            static (spc, source) => GenerateContextSharedSources(source, spc));
    }

    static void GenerateContextSharedSources(
        (bool, (EntitasGeneratorOptions, ((ImmutableArray<ContextData>, ImmutableArray<ComponentData>), ImmutableDictionary<string, ImmutableArray<ComponentData>>))) input,
        SourceProductionContext spc)
    {
        if (!input.Item1) // check shouldRun
            return;

        var options = input.Item2.Item1;
        var contextsData = input.Item2.Item2.Item1.Item1;
        var contextLookup = contextsData.ToDictionary(ctx => ctx.ContextName);

        var componentsByContextNameLookup = input.Item2.Item2.Item2;

        if (options.ComponentsLookupGenerationEnabled)
            ComponentsLookupGenerationHelper.GenerateComponentsLookups(spc, componentsByContextNameLookup, contextLookup, options);

        if (options.ComponentEntityIndexGenerationEnabled)
            EntityIndexGenerationHelper.GenerateEntityIndices(spc, componentsByContextNameLookup, contextLookup);

        if (options.CleanupGenerationEnabled)
            CleanupGenerationHelper.GenerateCleanupSystems(spc, componentsByContextNameLookup, contextLookup);

        if (options.ComponentEventSystemsGenerationEnabled)
            EventsGenerationHelper.GenerateEventSystems(spc, componentsByContextNameLookup, contextLookup);
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
                        .Select(component => (contextData, component, contextRootIsLocal));
                });
            });

        var conditionalComponentByContext = componentByContext
            .Combine(generatorOptions)
            .Combine(shouldRun)
            .Select((pair, _) => (ShouldRun: pair.Right, Options: pair.Left.Right, ComponentByContext: pair.Left.Left));

        // This is triggered for:
        // - individual components that have changed, or
        // - all components belonging to a context that has changed.
        context.RegisterSourceOutput(conditionalComponentByContext,
            static (spc, source) => GenerateComponentOwnedSources(source, spc));
    }

    static bool ShouldGenerateFeatureOwnedPlainApis(in ComponentData componentData) =>
        componentData.HasExplicitContexts && !componentData.IsGenerated;

    static void GenerateComponentOwnedSources(
        (bool ShouldRun, EntitasGeneratorOptions Options, (ContextData ContextData, ComponentData ComponentData, bool ContextRootIsLocal) ComponentByContext) input,
        SourceProductionContext spc)
    {
        if (!input.ShouldRun)
            return;

        var componentData = input.ComponentByContext.ComponentData;
        var contextData = input.ComponentByContext.ContextData;

        if (!input.ComponentByContext.ContextRootIsLocal)
        {
            ComponentGenerationHelper.GeneratePlainComponentApis(spc, componentData, input.Options, contextData);
            return;
        }

        GenerateSlotDependentComponentSources(spc, componentData, input.Options, contextData);

        if (componentData.IsGenerated)
            return;

        GeneratePerComponentEventAndCleanupSources(spc, componentData, input.Options, contextData);
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

        var combinedInput = shouldRun.Combine(contextsData);
        context.RegisterSourceOutput(combinedInput,
            static (spc, source) => GenerateVisualDebugging(source, spc));
    }

    static void GenerateVisualDebugging(
        (bool, ImmutableArray<ContextData>) input,
        SourceProductionContext spc)
    {
        if (!input.Item1) // check shouldRun
            return;

        var contextsData = input.Item2;
        if (contextsData.IsDefaultOrEmpty)
            return;

        // Context Observers, Feature
        VisualDebuggingGenerationHelper.Generate(spc, contextsData);
    }
}
