using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Entitas.CodeGeneration.Components.Data;
using Entitas.CodeGeneration.Contexts.Data;
using Entitas.CodeGeneration.EntityIndex.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Entitas.CodeGeneration.Contexts;

public static class ContextRegistrationGenerationHelper
{
    public static void GenerateContextRegistrations(
        SourceProductionContext spc,
        ImmutableArray<ContextData> contextsData,
        ImmutableDictionary<string, ImmutableArray<ComponentData>> componentsByContextNameLookup,
        in EntitasGeneratorOptions options)
    {
        if (!options.ContextGenerationEnabled || !options.ComponentContextExtensionGenerationEnabled)
            return;

        foreach (var contextData in contextsData)
        {
            componentsByContextNameLookup.TryGetValue(contextData.ContextName, out var componentsData);
            var hasEntityIndices = options.ComponentEntityIndexGenerationEnabled
                && !componentsData.IsDefaultOrEmpty
                && componentsData.Any(static componentData => componentData.GetEntityIndexCount() > 0);

            GenerateContextRegistration(spc, contextData, hasEntityIndices);
        }
    }

    static void GenerateContextRegistration(
        SourceProductionContext spc,
        in ContextData contextData,
        bool hasEntityIndices)
    {
        var initializeEntityIndices = hasEntityIndices
            ? $"\n        global::Entitas.{contextData.ContextName}ContextsEntityIndexExtension.Initialize{contextData.ContextName}EntityIndices(contexts);"
            : string.Empty;

        var source = $$"""
namespace Entitas
{
public static class {{contextData.ContextName}}ContextsRegistrationExtension
{
    public static global::Entitas.Contexts Register{{contextData.ContextName}}(this global::Entitas.Contexts contexts)
    {
        contexts.Register(new global::{{contextData.ContextTypeName}}());{{initializeEntityIndices}}
        return contexts;
    }

    public static global::Entitas.Contexts Register{{contextData.ContextName}}(this global::Entitas.Contexts contexts, global::Entitas.ContextSchema schema)
    {
        if (schema == null)
            throw new global::System.ArgumentNullException(nameof(schema));

        contexts.Register(new global::{{contextData.ContextTypeName}}(schema), schema);
        return contexts;
    }
}
}
""";

        spc.AddSource(contextData.ContextName + "ContextsRegistrationExtension.g.cs", SourceText.From(source, Encoding.UTF8));
    }
}
