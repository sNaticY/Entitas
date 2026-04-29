using System.Collections.Immutable;
using Entitas.CodeGeneration.Components.Data;
using Entitas.CodeGeneration.Contexts;
using Entitas.CodeGeneration.Extensions;
using Microsoft.CodeAnalysis;

namespace Entitas.CodeGeneration.Components.Helpers;

public static class ComponentAttributesHelper
{
    const string AttributeSuffix = "Attribute";
    const string EventAttributeTypeName = "Entitas.CodeGeneration.Attributes.EventAttribute";
    const string UniqueAttributeTypeName = "Entitas.CodeGeneration.Attributes.UniqueAttribute";
    const string FlagPrefixAttributeTypeName = "Entitas.CodeGeneration.Attributes.FlagPrefixAttribute";
    const string CleanupAttributeTypeName = "Entitas.CodeGeneration.Attributes.CleanupAttribute";

    public static void ParseComponentAttributes(INamedTypeSymbol type, 
        out ImmutableArray<string> contextNames,
        out ImmutableArray<EventData> events,
        out string flagPrefix,
        out bool isUnique,
        out bool hasCleanupAttribute,
        out CleanupMode cleanupMode,
        out bool hasExplicitContexts)
    {
        var contextNamesBuilder = ImmutableArray.CreateBuilder<string>();
        var eventsDataBuilder = ImmutableArray.CreateBuilder<EventData>();
        flagPrefix = "is";
        isUnique = false;
        hasCleanupAttribute = false;
        cleanupMode = CleanupMode.DestroyEntity;

        var attributes = type.GetAttributes();
        foreach (var attribute in attributes)
        {
            var attrClass = attribute.AttributeClass;
            if (attrClass == null)
                continue;
            
            var attributeName = attrClass.ToDisplayString();
            if (attributeName == UniqueAttributeTypeName)
            {
                isUnique = true;
            }
            else if (attributeName == CleanupAttributeTypeName)
            {
                hasCleanupAttribute = true;
                cleanupMode = GetCleanupMode(attribute);
            }
            else if (TryGetEventData(attribute, attributeName, out var eventData))
            {
                eventsDataBuilder.Add(eventData);
            }
            else if (TryGetContextName(attribute, attrClass, attributeName, out var contextName))
            {
                contextNamesBuilder.Add(contextName);
            }
            else if (TryGetFlagPrefix(attribute, attributeName, out var flagPrefixOverride))
            {
                flagPrefix = flagPrefixOverride;
            }
        }

        hasExplicitContexts = contextNamesBuilder.Count != 0;

        if (!hasExplicitContexts)
        {
            contextNamesBuilder.Add(ContextGenerationHelper.DefaultContextName);
            hasExplicitContexts = true;
        }
        
        contextNames = contextNamesBuilder.ToImmutable();
        events = eventsDataBuilder.ToImmutable();
    }
    
    public static bool TryGetContextName(AttributeData attribute, 
        INamedTypeSymbol attrClass, 
        string attributeName, 
        out string contextNameResult)
    {
        contextNameResult = string.Empty;
        
        // Direct usage of [Context("Name")]
        if (attributeName == ContextGenerationHelper.ContextAttributeTypeName)
        {
            if (attribute.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0].Value is string contextName)
            {
                contextNameResult = contextName;
                return true;
            }
            return false;
        }
        
        // Derived usage: [Game], where GameAttribute : ContextAttribute
        if (attrClass.BaseType?.ToDisplayString() == ContextGenerationHelper.ContextAttributeTypeName)
        {
            var attrName = attrClass.Name;
            if (!attrName.EndsWith(AttributeSuffix))
                return false;

            contextNameResult = attrName.RemoveLast(AttributeSuffix);
            return true;
        }

        return false;
    }

    public static CleanupMode GetCleanupMode(AttributeData attribute)
    {
        var args = attribute.ConstructorArguments;
        if (args.Length > 0 && args[0].Value is int cleanupModeInt)
            return (CleanupMode) cleanupModeInt;
        
        return CleanupMode.DestroyEntity;
    }

    public static bool TryGetEventData(AttributeData attribute,
        string attributeName, 
        out EventData eventDataResult)
    {
        eventDataResult = default;
        
        // ex: [Event(EventTarget.Self)]
        if (attributeName != EventAttributeTypeName)
            return false;

        var args = attribute.ConstructorArguments;
        if (args.Length == 0)
            return false;
        
        EventTarget eventTarget = default;
        EventType eventType = EventType.Added; // default value from constructor
        int priority = 0; // default value from constructor

        // arg[0]: EventTarget (required)
        if (args[0].Value is int targetInt)
            eventTarget = (EventTarget)targetInt;

        // arg[1]: EventType (optional)
        if (args.Length > 1 && args[1].Value is int typeInt)
            eventType = (EventType)typeInt;

        // arg[2]: Priority (optional)
        if (args.Length > 2 && args[2].Value is int p)
            priority = p;

        eventDataResult = new EventData(eventTarget, eventType, priority);
        return true;
    }

    public static bool TryGetFlagPrefix(AttributeData attribute,
        string attributeName,
        out string flagPrefixResult)
    {
        flagPrefixResult = string.Empty;

        if (attributeName != FlagPrefixAttributeTypeName)
            return false;
            
        var args = attribute.ConstructorArguments;
        if (args.Length == 0)
            return false;

        if (args[0].Value is string flagPrefix)
        {
            flagPrefixResult = flagPrefix;
            return true;
        }
        
        return false;
    }
}
