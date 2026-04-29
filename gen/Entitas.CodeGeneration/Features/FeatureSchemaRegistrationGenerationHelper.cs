using System.Collections.Immutable;
using System.Text;
using Entitas.CodeGeneration.Cleanup;
using Entitas.CodeGeneration.Components;
using Entitas.CodeGeneration.Components.Data;
using Entitas.CodeGeneration.Components.Extensions;
using Entitas.CodeGeneration.Contexts.Data;
using Entitas.CodeGeneration.EntityIndex.Extensions;
using Entitas.CodeGeneration.Events.Extensions;
using Entitas.CodeGeneration.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Entitas.CodeGeneration.Features;

public static class FeatureSchemaRegistrationGenerationHelper
{
    public static void GenerateFeatureSchemaRegistrations(
        SourceProductionContext spc,
        ImmutableDictionary<string, ImmutableArray<ComponentData>> componentsByContextNameLookup,
        Dictionary<string, ContextData> localContextLookup,
        in EntitasGeneratorOptions options)
    {
        var featureContexts = componentsByContextNameLookup
            .Where(pair => !localContextLookup.ContainsKey(pair.Key))
            .Where(pair => pair.Value.Any(static component => component.HasExplicitContexts))
            .OrderBy(pair => pair.Key)
            .ToArray();

        var includeContextNameInMethod = featureContexts.Length > 1;
        foreach (var pair in featureContexts)
        {
            var contextData = new ContextData(pair.Key);
            GenerateFeatureSchemaRegistration(spc, contextData, pair.Value, options, includeContextNameInMethod);
        }
    }

    static void GenerateFeatureSchemaRegistration(
        SourceProductionContext spc,
        in ContextData contextData,
        ImmutableArray<ComponentData> componentsData,
        in EntitasGeneratorOptions options,
        bool includeContextNameInMethod)
    {
        var methodPrefix = includeContextNameInMethod ? contextData.ContextName : string.Empty;
        var extensionType = contextData.ContextName + options.AssemblyName + "SchemaExtensions";
        var methodName = "Add" + methodPrefix + options.AssemblyName + "Schema";
        var calls = GetSchemaRegistrationCalls(contextData, componentsData, options);

        if (calls.Length == 0)
            return;

        var source = $$"""
public static class {{extensionType}}
{
    public static global::Entitas.ContextSchemaBuilder {{methodName}}(this global::Entitas.ContextSchemaBuilder builder)
    {
{{calls}}
        return builder;
    }
}
""";

        spc.AddSource(extensionType + ".g.cs", SourceText.From(source, Encoding.UTF8));
    }

    static string GetSchemaRegistrationCalls(
        in ContextData contextData,
        ImmutableArray<ComponentData> componentsData,
        in EntitasGeneratorOptions options)
    {
        var builder = new StringBuilder();
        foreach (var componentData in componentsData.OrderBy(static component => component.FullTypeName))
        {
            if (options.ComponentComponentIndexGenerationEnabled)
                AppendCall(builder, componentData.Namespace, contextData.ContextName + componentData.GetScopedComponentName() + "ComponentSchemaExtensions", "Add" + contextData.ContextName + componentData.GetScopedComponentName());

            if (componentData.IsGenerated)
                continue;

            if (options.ComponentEntityIndexGenerationEnabled && componentData.GetEntityIndexCount() > 0)
                AppendCall(builder, componentData.Namespace, contextData.ContextName + componentData.GetScopedComponentName() + "EntityIndicesSchemaExtensions", "Add" + contextData.ContextName + componentData.GetScopedComponentName() + "EntityIndices");

            if (options.CleanupGenerationEnabled && componentData.HasCleanupAttribute)
            {
                var cleanupSystemName = (componentData.CleanupMode == CleanupMode.DestroyEntity ? "Destroy" : "Remove") + componentData.GetComponentName() + contextData.SystemTypeName;
                AppendCall(builder, componentData.Namespace, cleanupSystemName + "SchemaExtensions", "Add" + cleanupSystemName);
            }

            if (options.ComponentEventSystemsGenerationEnabled && componentData.HasEvents)
            {
                foreach (var eventData in componentData.Events.OrderBy(static eventData => eventData.Priority))
                {
                    var eventSystemName = componentData.EventName(contextData.ContextName, eventData) + "EventSystem";
                    AppendCall(builder, componentData.Namespace, eventSystemName + "SchemaExtensions", "Add" + eventSystemName);
                }
            }
        }

        return builder.ToString().TrimEnd();
    }

    static void AppendCall(StringBuilder builder, string? namespaceName, string typeName, string methodName)
    {
        builder.Append("        builder = ")
            .Append(typeName.Qualify(namespaceName, global: true))
            .Append('.')
            .Append(methodName)
            .AppendLine("(builder);");
    }
}
