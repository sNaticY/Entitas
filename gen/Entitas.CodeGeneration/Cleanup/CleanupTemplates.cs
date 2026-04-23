using Entitas.CodeGeneration.Components.Data;
using Entitas.CodeGeneration.Components.Extensions;
using Entitas.CodeGeneration.Components;
using Entitas.CodeGeneration.Contexts.Data;
using Entitas.CodeGeneration.Extensions;

namespace Entitas.CodeGeneration.Cleanup;

public static class CleanupTemplates
{
    public const string CleanupSystemsTemplate =
        @"public sealed class ${ContextName}CleanupSystems : Entitas.Systems
{
    public ${ContextName}CleanupSystems(global::Entitas.Contexts contexts)
    {
${systemsList}
    }
}
";

    public const string DestroyEntityCleanupSystemTemplate =
        @"using System.Collections.Generic;
using Entitas;

public sealed class Destroy${CleanupSystemComponentName}${SystemType} : ICleanupSystem
{
    readonly IGroup<${EntityType}> _group;
    readonly List<${EntityType}> _buffer = new List<${EntityType}>();

    public Destroy${CleanupSystemComponentName}${SystemType}(global::Entitas.Contexts contexts)
    {
        _group = contexts.Get${ContextName}().GetGroup(global::Entitas.Matcher<${EntityType}>.AllOf(${ComponentHandle}));
    }

    public void Cleanup()
    {
        foreach (var e in _group.GetEntities(_buffer))
        {
            e.Destroy();
        }
    }
}
";

    public static string GetDestroyEntityCleanupSystemSource(
        in ContextData contextData,
        in ComponentData componentData,
        out string fileName)
    {
        var cleanupSystemComponentName = componentData.GetComponentName();
        var matcherComponentName = componentData.GetComponentName();
        fileName = "Destroy" + cleanupSystemComponentName + contextData.SystemTypeName;

        return DestroyEntityCleanupSystemTemplate
            .Replace("${CleanupSystemComponentName}", cleanupSystemComponentName)
            .Replace("${ComponentHandle}", ComponentTemplates.GetComponentHandleExpression(contextData, componentData))
            .Replace("${MatcherComponentName}", matcherComponentName)
            .Replace("${ContextName}", contextData.ContextName)
            .Replace("${ContextType}", contextData.ContextTypeName)
            .Replace("${SystemType}", contextData.SystemTypeName)
            .Replace("${EntityType}", contextData.EntityTypeName)
            .Replace("${MatcherType}", contextData.MatcherTypeName);
    }

    public const string RemoveComponentCleanupSystemTemplate =
        @"using System.Collections.Generic;
using Entitas;

public sealed class Remove${CleanupSystemComponentName}${SystemType} : ICleanupSystem
{
    readonly IGroup<${EntityType}> _group;
    readonly List<${EntityType}> _buffer = new List<${EntityType}>();

    public Remove${CleanupSystemComponentName}${SystemType}(global::Entitas.Contexts contexts)
    {
        _group = contexts.Get${ContextName}().GetGroup(global::Entitas.Matcher<${EntityType}>.AllOf(${ComponentHandle}));
    }

    public void Cleanup()
    {
        foreach (var e in _group.GetEntities(_buffer))
        {
            e.${removeComponent};
        }
    }
}
";

    public static string GetRemoveComponentCleanupSystemSource(
        in ContextData contextData,
        in ComponentData componentData,
        out string fileName)
    {
        var cleanupSystemComponentName = componentData.GetComponentName();
        var actionComponentName = componentData.GetScopedComponentName();
        var matcherComponentName = componentData.GetComponentName();
        fileName = "Remove" + cleanupSystemComponentName + contextData.SystemTypeName;

        var removeComponentSource = componentData.Members.Length == 0
            ? $"Set{actionComponentName}(false)"
            : $"Remove{actionComponentName}()";

        return RemoveComponentCleanupSystemTemplate
            .Replace("${CleanupSystemComponentName}", cleanupSystemComponentName)
            .Replace("${ComponentHandle}", ComponentTemplates.GetComponentHandleExpression(contextData, componentData))
            .Replace("${MatcherComponentName}", matcherComponentName)
            .Replace("${ContextName}", contextData.ContextName)
            .Replace("${ContextType}", contextData.ContextTypeName)
            .Replace("${SystemType}", contextData.SystemTypeName)
            .Replace("${EntityType}", contextData.EntityTypeName)
            .Replace("${MatcherType}", contextData.MatcherTypeName)
            .Replace("${removeComponent}", removeComponentSource);
    }
}
