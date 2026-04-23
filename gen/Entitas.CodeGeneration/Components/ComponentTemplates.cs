using Entitas.CodeGeneration.Components.Data;
using Entitas.CodeGeneration.Components.Extensions;
using Entitas.CodeGeneration.Contexts.Data;
using Entitas.CodeGeneration.Extensions;

namespace Entitas.CodeGeneration.Components;

public static class ComponentTemplates
{
    const string ComponentHandleMemberName = "Handle";

    public const string GeneratedComponentTemplate =
        @"[Entitas.CodeGeneration.Attributes.DontGenerate]
public sealed class ${FullComponentName} : Entitas.IComponent
{
    public ${Type} value;
}
";

    const string ComponentHandleTemplate =
        @"public static class ${ComponentHandleType}
{
    public static readonly Entitas.ComponentHandle<${ComponentType}> ${ComponentHandleMember} = new Entitas.ComponentHandle<${ComponentType}>(""${ComponentName}"");
}
";

    public static string GetComponentHandleSource(
        in ContextData contextData,
        in ComponentData componentData)
    {
        return ComponentHandleTemplate
            .Replace("${ComponentHandleType}", GetComponentHandleTypeName(contextData, componentData))
            .Replace("${ComponentHandleMember}", ComponentHandleMemberName)
            .Replace("${ComponentType}", componentData.FullTypeName)
            .Replace("${ComponentName}", componentData.GetComponentName());
    }

    public static string GetComponentHandleExpression(
        in ContextData contextData,
        in ComponentData componentData) =>
        GetComponentHandleTypeName(contextData, componentData) + "." + ComponentHandleMemberName;

    public static string GetGlobalComponentHandleExpression(
        in ContextData contextData,
        in ComponentData componentData)
    {
        var expression = GetComponentHandleExpression(contextData, componentData);
        return componentData.Namespace is null
            ? expression
            : "global::" + componentData.Namespace + "." + expression;
    }

    static string GetComponentHandleTypeName(
        in ContextData contextData,
        in ComponentData componentData) =>
        contextData.ContextName + componentData.GetScopedComponentName() + "ComponentHandle";

    const string StandardComponentContextApiTemplate =
        @"public static class ${ContextExtensionsType}
{
    public static ${EntityType} ${getComponentEntity}(this ${ContextType} context) { return context.GetGroup(global::Entitas.Matcher<${EntityType}>.AllOf(${Handle})).GetSingleEntity(); }
    public static ${ComponentType} ${getComponent}(this ${ContextType} context) { return context.${getComponentEntity}().${getComponent}(); }
    public static bool ${hasComponent}(this ${ContextType} context) { return context.${getComponentEntity}() != null; }

    public static ${EntityType} Set${ApiComponentName}(this ${ContextType} context, ${newMethodParameters})
    {
        if (context.${hasComponent}())
        {
            throw new Entitas.EntitasException(""Could not set ${ApiComponentName}!\n"" + context + "" already has an entity with ${ComponentType}!"",
                ""You should check if the context already has a ${getComponentEntity}() before setting it or use context.Replace${ApiComponentName}()."");
        }
        var entity = context.CreateEntity();
        entity.Add${ApiComponentName}(${newMethodArgs});
        return entity;
    }

    public static void Replace${ApiComponentName}(this ${ContextType} context, ${newMethodParameters})
    {
        var entity = context.${getComponentEntity}();
        if (entity == null)
        {
            entity = context.Set${ApiComponentName}(${newMethodArgs});
        }
        else
        {
            entity.Replace${ApiComponentName}(${newMethodArgs});
        }
    }

    public static void Remove${ApiComponentName}(this ${ContextType} context)
    {
        context.${getComponentEntity}().Destroy();
    }
}
";

    public static string GetStandardComponentContextApiSource(
        in ContextData contextData,
        in ComponentData componentData)
    {
        var apiComponentName = componentData.GetScopedComponentName();
        var matcherComponentName = componentData.GetComponentName();
        var newMethodParameters = componentData.Members.GetMethodParameters(true);
        var newMethodArgs = componentData.Members.GetMethodArgs(true);
        var contextExtensionsType = contextData.ContextName + componentData.GetScopedComponentName() + "ContextExtensions";
        var componentHandle = GetComponentHandleExpression(contextData, componentData);

        return StandardComponentContextApiTemplate
            .Replace("${ContextExtensionsType}", contextExtensionsType)
            .Replace("${ContextType}", contextData.ContextTypeName)
            .Replace("${EntityType}", contextData.EntityTypeName)
            .Replace("${ApiComponentName}", apiComponentName)
            .Replace("${Handle}", componentHandle)
            .Replace("${MatcherComponentName}", matcherComponentName)
            .Replace("${getComponentEntity}", componentData.GetUniqueEntityGetterMethodName())
            .Replace("${getComponent}", componentData.GetComponentGetterMethodName())
            .Replace("${hasComponent}", componentData.GetHasComponentMethodName())
            .Replace("${MatcherType}", contextData.MatcherTypeName)
            .Replace("${ComponentType}", componentData.FullTypeName)
            .Replace("${newMethodParameters}", newMethodParameters)
            .Replace("${newMethodArgs}", newMethodArgs);
    }

    const string FlagComponentContextApiTemplate =
        @"public static class ${ContextExtensionsType}
{
    public static ${EntityType} ${getComponentEntity}(this ${ContextType} context) { return context.GetGroup(global::Entitas.Matcher<${EntityType}>.AllOf(${Handle})).GetSingleEntity(); }

    public static bool ${flagCheck}(this ${ContextType} context)
    {
        return context.${getComponentEntity}() != null;
    }

    public static void ${flagSet}(this ${ContextType} context, bool value)
    {
        var entity = context.${getComponentEntity}();
        if (value != (entity != null))
        {
            if (value)
            {
                context.CreateEntity().${flagSet}(true);
            }
            else
            {
                entity.Destroy();
            }
        }
    }
}
";

    // (unique) components without members
    public static string GetFlagComponentContextApiSource(
        in ContextData contextData,
        in ComponentData componentData)
    {
        var matcherComponentName = componentData.GetComponentName();
        var contextExtensionsType = contextData.ContextName + componentData.GetScopedComponentName() + "ContextExtensions";
        var componentHandle = GetComponentHandleExpression(contextData, componentData);

        return FlagComponentContextApiTemplate
            .Replace("${ContextExtensionsType}", contextExtensionsType)
            .Replace("${ContextType}", contextData.ContextTypeName)
            .Replace("${EntityType}", contextData.EntityTypeName)
            .Replace("${Handle}", componentHandle)
            .Replace("${MatcherComponentName}", matcherComponentName)
            .Replace("${getComponentEntity}", componentData.GetUniqueEntityGetterMethodName())
            .Replace("${flagCheck}", componentData.GetFlagCheckMethodName())
            .Replace("${flagSet}", componentData.GetFlagSetMethodName())
            .Replace("${MatcherType}", contextData.MatcherTypeName)
            .Replace("${prefixedComponentName}", componentData.PrefixedComponentName());
    }

    const string StandardComponentEntityApiTemplate =
        @"public static class ${EntityExtensionsType}
{
    public static ${ComponentType} ${getComponent}(this ${EntityType} entity) { return (${ComponentType})entity.GetComponent(${Handle}); }
    public static bool ${hasComponent}(this ${EntityType} entity) { return entity.HasComponent(${Handle}); }

    public static void Add${ComponentName}(this ${EntityType} entity, ${newMethodParameters})
    {
        var handle = ${Handle};
        var component = (${ComponentType})entity.CreateComponent(handle, typeof(${ComponentType}));
${memberAssignmentList}
        entity.AddComponent(handle, component);
    }

    public static void Replace${ComponentName}(this ${EntityType} entity, ${newMethodParameters})
    {
        var handle = ${Handle};
        var component = (${ComponentType})entity.CreateComponent(handle, typeof(${ComponentType}));
${memberAssignmentList}
        entity.ReplaceComponent(handle, component);
    }

    public static void Remove${ComponentName}(this ${EntityType} entity)
    {
        entity.RemoveComponent(${Handle});
    }
}
";

    public static string GetStandardComponentEntityApiSource(
        in ContextData contextData,
        in ComponentData componentData)
    {
        var componentName = componentData.GetScopedComponentName();
        var componentHandle = GetComponentHandleExpression(contextData, componentData);
        var newMethodParameters = componentData.Members.GetMethodParameters(true);
        var memberAssignmentList = componentData.Members.GetMemberAssignmentList();
        var entityExtensionsType = contextData.ContextName + componentData.GetScopedComponentName() + "EntityExtensions";

        return StandardComponentEntityApiTemplate
            .Replace("${EntityExtensionsType}", entityExtensionsType)
            .Replace("${EntityType}", contextData.EntityTypeName)
            .Replace("${ComponentType}", componentData.FullTypeName)
            .Replace("${ComponentName}", componentName)
            .Replace("${Handle}", componentHandle)
            .Replace("${getComponent}", componentData.GetComponentGetterMethodName())
            .Replace("${hasComponent}", componentData.GetHasComponentMethodName())
            .Replace("${newMethodParameters}", newMethodParameters)
            .Replace("${memberAssignmentList}", memberAssignmentList);
    }

    const string FlagComponentEntityApiTemplate =
        @"public static class ${EntityExtensionsType}
{
    static readonly ${ComponentType} ${componentName}Component = new ${ComponentType}();

    public static bool ${flagCheck}(this ${EntityType} entity)
    {
        return entity.HasComponent(${Handle});
    }

    public static void ${flagSet}(this ${EntityType} entity, bool value)
    {
        if (value != entity.${flagCheck}())
        {
            var handle = ${Handle};
            if (value)
            {
                var componentPool = entity.GetComponentPool(handle);
                var component = componentPool.Count > 0
                        ? componentPool.Pop()
                        : ${componentName}Component;

                entity.AddComponent(handle, component);
            }
            else
            {
                entity.RemoveComponent(handle);
            }
        }
    }
}
";

    public static string GetFlagComponentEntityApiSource(
        in ContextData contextData,
        in ComponentData componentData)
    {
        return FlagComponentEntityApiTemplate
            .Replace("${EntityExtensionsType}", contextData.ContextName + componentData.GetScopedComponentName() + "EntityExtensions")
            .Replace("${EntityType}", contextData.EntityTypeName)
            .Replace("${ComponentType}", componentData.FullTypeName)
            .Replace("${componentName}", componentData.GetScopedComponentNameLowerFirst())
            .Replace("${Handle}", GetComponentHandleExpression(contextData, componentData))
            .Replace("${flagCheck}", componentData.GetFlagCheckMethodName())
            .Replace("${flagSet}", componentData.GetFlagSetMethodName());
    }

    const string ComponentMatcherApiTemplate =
        @"public sealed partial class ${MatcherType}
{
    static Entitas.IMatcher<${EntityType}> _matcher${ComponentName};

    public static Entitas.IMatcher<${EntityType}> ${ComponentName}()
    {
        if (_matcher${ComponentName} == null)
        {
            var matcher = (Entitas.Matcher<${EntityType}>)Entitas.Matcher<${EntityType}>.AllOf(${Index});
            matcher.ComponentNames = ${componentNames};
            _matcher${ComponentName} = matcher;
        }

        return _matcher${ComponentName};
    }
}
";

    public static string GetComponentMatcherApiSource(
        in ContextData contextData,
        in ComponentData componentData)
    {
        var entityType = contextData.EntityTypeName;
        var matcherType = contextData.MatcherTypeName;
        var componentName = componentData.GetComponentName();
        var componentIndex = componentData.GetComponentIndex(contextData);
        var componentNames = $"{contextData.ContextName}{ComponentGenerationHelper.ComponentsLookupName}.componentNames";

        return ComponentMatcherApiTemplate
            .Replace("${MatcherType}", matcherType)
            .Replace("${ComponentName}", componentName)
            .Replace("${Index}", componentIndex)
            .Replace("${componentNames}", componentNames)
            .Replace("${EntityType}", entityType);
    }
}
