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
        RegisterContextsGeneration(context, shouldRun, generatorOptions, contextsData);

        var componentsData = ComponentGenerationHelper.GetComponentsData(context, generatorOptions);
        var componentsByContextNameLookup = ComponentsLookupGenerationHelper.GetComponentsByContextNameLookup(componentsData);
        RegisterIndividualComponentsGeneration(context, shouldRun, generatorOptions, contextsData, componentsByContextNameLookup);
        RegisterSharedSourcesGeneration(context, shouldRun, generatorOptions, contextsData, componentsData, componentsByContextNameLookup);

        RegisterVisualDebuggingGeneration(context, generatorOptions, contextsData);
    }

    void RegisterContextsGeneration(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<bool> shouldRun,
        IncrementalValueProvider<EntitasGeneratorOptions> generatorOptions,
        in IncrementalValueProvider<ImmutableArray<ContextData>> contextsData)
    {
        var combinedInput = shouldRun.Combine(generatorOptions.Combine(contextsData));

        // This will be triggered for any context change (ContextAttribute class)
        context.RegisterSourceOutput(combinedInput,
            static (spc, source) => GenerateContexts(source, spc));
    }

    static void GenerateContexts(
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

    void RegisterSharedSourcesGeneration(
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
            static (spc, source) => GenerateSharedSources(source, spc));
    }

    static void GenerateSharedSources(
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
            ComponentsLookupGenerationHelper.GenerateComponentsLookups(spc, componentsByContextNameLookup, contextLookup);

        if (options.ComponentEntityIndexGenerationEnabled)
            EntityIndexGenerationHelper.GenerateEntityIndices(spc, componentsByContextNameLookup, contextLookup);

        if (options.CleanupGenerationEnabled)
            CleanupGenerationHelper.GenerateCleanupSystems(spc, componentsByContextNameLookup, contextLookup);

        if (options.ComponentEventSystemsGenerationEnabled)
            EventsGenerationHelper.GenerateEventSystems(spc, componentsByContextNameLookup, contextLookup);
    }

    void RegisterIndividualComponentsGeneration(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<bool> shouldRun,
        IncrementalValueProvider<EntitasGeneratorOptions> generatorOptions,
        in IncrementalValueProvider<ImmutableArray<ContextData>> contextsData,
        in IncrementalValueProvider<ImmutableDictionary<string, ImmutableArray<ComponentData>>> componentsByContextNameLookup)
    {
        // Extract all (ContextData, ComponentData) pairs
        var componentByContext = contextsData
            .Combine(componentsByContextNameLookup)
            .SelectMany((pair, _) =>
            {
                var (contexts, componentLookup) = pair;
                return contexts.SelectMany((contextData, _) =>
                {
                    return !componentLookup.TryGetValue(contextData.ContextName, out var matchingComponents)
                        ? Enumerable.Empty<(ContextData, ComponentData)>()
                        : matchingComponents.Select(component => (contextData, component));
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
            static (spc, source) => GenerateIndividualEntityComponent(source, spc));
    }

    static void GenerateIndividualEntityComponent(
        (bool ShouldRun, EntitasGeneratorOptions Options, (ContextData, ComponentData) ComponentByContext) input,
        SourceProductionContext spc)
    {
        if (!input.ShouldRun)
            return;

        var componentData = input.ComponentByContext.Item2;
        var contextData = input.ComponentByContext.Item1;

        ComponentGenerationHelper.GenerateEntityComponent(spc, componentData, input.Options, contextData);

        if (componentData.IsGenerated)
            return;

        if (input.Options.ComponentEventsGenerationEnabled)
            EventsGenerationHelper.GenerateComponentEvents(spc, componentData, contextData);

        if (input.Options.CleanupGenerationEnabled && componentData.HasCleanupAttribute)
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
