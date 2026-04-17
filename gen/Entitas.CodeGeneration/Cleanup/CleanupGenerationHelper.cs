using System.Collections.Immutable;
using System.Text;
using Entitas.CodeGeneration.Components.Data;
using Entitas.CodeGeneration.Contexts.Data;
using Entitas.CodeGeneration.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Entitas.CodeGeneration.Cleanup;

public static class CleanupGenerationHelper
{
    public static void GenerateCleanupSystems(SourceProductionContext spc, 
        ImmutableDictionary<string, ImmutableArray<ComponentData>> componentsByContextNameLookup,
        Dictionary<string, ContextData> contextLookup)
    {
        foreach (var contextComponentsPair in componentsByContextNameLookup)
        {
            var contextName = contextComponentsPair.Key;
            if (!contextLookup.TryGetValue(contextName, out var contextData))
                continue;

            var componentArray = contextComponentsPair.Value;
            GenerateCleanupSystems(spc, contextData, componentArray);
        }
    }

    public static void GenerateCleanupSystems(SourceProductionContext spc,
        in ContextData contextData,
        in ImmutableArray<ComponentData> componentsData)
    {
        var contextSystemName = contextData.SystemTypeName;
        
        var systemsList = string.Join("\n", componentsData
            .Where(c => c.HasCleanupAttribute)
            .Select(c =>
            {
                var systemTypeName =
                    (c.CleanupMode == CleanupMode.DestroyEntity ? "Destroy" : "Remove") +
                    c.GetComponentName() + contextSystemName;

                if (!string.IsNullOrWhiteSpace(c.Namespace))
                    systemTypeName = "global::" + c.Namespace + "." + systemTypeName;

                return "        Add(new " + systemTypeName + "(contexts));";
            }));

        var componentSource = CleanupTemplates.CleanupSystemsTemplate
            .Replace("${ContextName}", contextData.ContextName)
            .Replace("${systemsList}", systemsList);
        
        spc.AddSource(contextData.ContextName + "CleanupSystems.g.cs", SourceText.From(componentSource, Encoding.UTF8));
    }

    public static void GenerateComponentCleanupSystem(SourceProductionContext spc,
        in ComponentData componentData,
        in ContextData contextData)
    {
        if (!componentData.HasCleanupAttribute)
            return;

        string filename;
        var source = componentData.CleanupMode switch
        {
            CleanupMode.DestroyEntity => CleanupTemplates.GetDestroyEntityCleanupSystemSource(contextData, componentData, out filename),
            CleanupMode.RemoveComponent => CleanupTemplates.GetRemoveComponentCleanupSystemSource(contextData, componentData, out filename),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        var fileName = filename.NamespacedHintName(componentData.Namespace);
        spc.AddSource(fileName + ".g.cs", SourceText.From(source.WrapInNamespace(componentData.Namespace), Encoding.UTF8));
    }
}
