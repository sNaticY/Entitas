using Entitas.CodeGeneration.Components.Data;
using Entitas.CodeGeneration.Components.Extensions;
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

public sealed class Destroy${ComponentName}${SystemType} : ICleanupSystem
{
    readonly IGroup<${EntityType}> _group;
    readonly List<${EntityType}> _buffer = new List<${EntityType}>();

    public Destroy${ComponentName}${SystemType}(global::Entitas.Contexts contexts)
    {
        _group = contexts.Get${ContextName}().GetGroup(${MatcherType}.${ComponentName}());
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
        var componentName = componentData.GetComponentName();
        fileName = "Destroy" + componentName + contextData.SystemTypeName;

        return DestroyEntityCleanupSystemTemplate
            .Replace("${ComponentName}", componentName)
            .Replace("${ContextName}", contextData.ContextName)
            .Replace("${ContextType}", contextData.ContextTypeName)
            .Replace("${SystemType}", contextData.SystemTypeName)
            .Replace("${EntityType}", contextData.EntityTypeName)
            .Replace("${MatcherType}", contextData.MatcherTypeName);
    }

    public const string RemoveComponentCleanupSystemTemplate =
        @"using System.Collections.Generic;
using Entitas;

public sealed class Remove${ComponentName}${SystemType} : ICleanupSystem
{
    readonly IGroup<${EntityType}> _group;
    readonly List<${EntityType}> _buffer = new List<${EntityType}>();

    public Remove${ComponentName}${SystemType}(global::Entitas.Contexts contexts)
    {
        _group = contexts.Get${ContextName}().GetGroup(${MatcherType}.${ComponentName}());
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
        var componentName = componentData.GetComponentName();
        fileName = "Remove" + componentName + contextData.SystemTypeName;

        var removeComponentSource = componentData.Members.Length == 0
            ? $"Set{componentName}(false)"
            : $"Remove{componentName}()";

        return RemoveComponentCleanupSystemTemplate
            .Replace("${ComponentName}", componentName)
            .Replace("${ContextName}", contextData.ContextName)
            .Replace("${ContextType}", contextData.ContextTypeName)
            .Replace("${SystemType}", contextData.SystemTypeName)
            .Replace("${EntityType}", contextData.EntityTypeName)
            .Replace("${MatcherType}", contextData.MatcherTypeName)
            .Replace("${removeComponent}", removeComponentSource);
    }
}
