using System.Collections.Immutable;
using System.Text;
using Entitas.CodeGeneration.Components.Data;
using Entitas.CodeGeneration.Components.Extensions;
using Entitas.CodeGeneration.Contexts.Data;
using Entitas.CodeGeneration.Events;
using Entitas.CodeGeneration.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Entitas.CodeGeneration.Components;

public static class ComponentGenerationHelper
{
    const string EntitasComponentTypeName = "IComponent";
    const string EntitasComponentFullTypeName = "Entitas."+EntitasComponentTypeName;

    public const string ComponentsLookupName = "ComponentsLookup";
    public const string ComponentName = "Component";

    public const string DontGenerateAttributeName = "DontGenerate";
    public const string DontGenerateAttributeFullName = DontGenerateAttributeName+"Attribute";

    // ignoreNamespaces was a config in Jenny.properties
    public static bool IgnoreNamespaces = false;
    
    public static IncrementalValueProvider<ImmutableArray<ComponentData>> GetComponentsData(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<EntitasGeneratorOptions> generatorOptions)
    {
        var declaredComponentsData = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsComponentCandidateSyntax(s),
                transform: static (ctx, _) => TryGetComponentType(ctx))
            .Where(static c => c is not null)
            .Select(static (c, _) => (ComponentData)c!) // null-forgiving operator, safe after filtering
            .Collect();

        // Include generated components (ex: events)
        return declaredComponentsData
            .Combine(generatorOptions)
            .Select(static (input, _) => MergeWithGeneratedComponents(input.Left, input.Right));
    }

    static ImmutableArray<ComponentData> MergeWithGeneratedComponents(
        ImmutableArray<ComponentData> originalComponents,
        in EntitasGeneratorOptions options)
    {
        var builder = ImmutableArray.CreateBuilder<ComponentData>(originalComponents.Length);
        builder.AddRange(originalComponents);

        if (!options.ComponentEventsGenerationEnabled)
            return builder.ToImmutable();

        // Add generated components (ex: events)
        foreach (var component in originalComponents)
        {
            if (!component.HasEvents)
                continue;

            EventsGenerationHelper.CreateEventComponents(component, builder);
        }

        return builder.ToImmutable();
    }
    
    static bool IsComponentCandidateSyntax(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax c)
            return false;

        // For optimal performance, we filter aggressively here...
        // so it's important to enforce the naming convention in Rider with warnings for violations 
        if (c.Identifier.Text.EndsWith(ComponentName))
        {
            return !HasDontGenerateAttribute(c);
        }
        
        return false;
    }

    static bool HasDontGenerateAttribute(ClassDeclarationSyntax c)
    {
        foreach (var attributeList in c.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name switch
                {
                    IdentifierNameSyntax id => id.Identifier.Text,
                    QualifiedNameSyntax q => q.Right.Identifier.Text,
                    _ => null,
                };

                if (attributeName is DontGenerateAttributeName or DontGenerateAttributeFullName) 
                    return true;
            }
        }
        return false;
    }
    
    static ComponentData? TryGetComponentType(GeneratorSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax) context.Node;
        
        if (context.SemanticModel.GetDeclaredSymbol(classSyntax) is not INamedTypeSymbol classTypeSymbol)
            return null;
        
        if (classTypeSymbol.IsAbstract)
            return null;
        
        foreach (var i in classTypeSymbol.AllInterfaces)
        {
            // avoid ToDisplayString() allocation as much as possible
            if (i?.Name == EntitasComponentTypeName && i.ToDisplayString() == EntitasComponentFullTypeName)
            {
                return new ComponentData(classTypeSymbol);
            }
        }

        return null;
    }
    
    public static void GeneratePlainComponentApis(SourceProductionContext spc,
        in ComponentData componentData,
        in EntitasGeneratorOptions options,
        in ContextData contextData)
    {
        var source = string.Empty;

        if (ShouldGenerateComponentHandle(componentData, options))
        {
            source += ComponentTemplates.GetComponentHandleSource(contextData, componentData) + "\n";
            source += ComponentTemplates.GetComponentSchemaRegistrationSource(contextData, componentData) + "\n";
        }

        if (componentData.IsUnique && options.ComponentContextExtensionGenerationEnabled)
            source += CreateComponentContextApiSource(componentData, contextData);

        if (options.ComponentEntityExtensionGenerationEnabled)
        {
            if (componentData.Members.Length == 0)
            {
                source += ComponentTemplates.GetFlagComponentEntityApiSource(contextData, componentData);
            }
            else
            {
                source += ComponentTemplates.GetStandardComponentEntityApiSource(contextData, componentData);
            }
        }

        if (!string.IsNullOrEmpty(source))
        {
            var fileName = GetPlainComponentApiHintName(componentData, contextData);

            spc.AddSource($"{fileName}.g.cs", SourceText.From(source.WrapInNamespace(componentData.Namespace), Encoding.UTF8));
        }
    }

    public static bool ShouldGenerateComponentHandle(
        in ComponentData componentData,
        in EntitasGeneratorOptions options) =>
        options.ComponentEntityExtensionGenerationEnabled
        || options.ComponentMatcherGenerationEnabled
        || (componentData.IsUnique && options.ComponentContextExtensionGenerationEnabled);

    public static void GenerateFeatureOwnedComponentMatcherApis(SourceProductionContext spc,
        ImmutableArray<ComponentData> componentsData,
        in EntitasGeneratorOptions options,
        in ContextData contextData)
    {
        if (!options.ComponentMatcherGenerationEnabled || componentsData.IsDefaultOrEmpty)
            return;

        foreach (var componentData in componentsData)
        {
            var matcherSource = ComponentTemplates.GetFeatureOwnedComponentMatcherApiSource(contextData, componentData);
            var matcherFileName = (contextData.ContextName + componentData.GetScopedComponentName() + "MatcherExtensions")
                .NamespacedHintName(componentData.Namespace);
            spc.AddSource($"{matcherFileName}.g.cs", SourceText.From(matcherSource.WrapInNamespace(componentData.Namespace), Encoding.UTF8));
        }
    }

    public static void GenerateComponentMatcherApi(SourceProductionContext spc,
        in ComponentData componentData,
        in EntitasGeneratorOptions options,
        in ContextData contextData)
    {
        if (!options.ComponentMatcherGenerationEnabled)
            return;

        var matcherSource = ComponentTemplates.GetComponentMatcherApiSource(contextData, componentData);
        var matcherFileName = (contextData.ContextName + componentData.GetScopedComponentName() + "MatcherExtensions")
            .NamespacedHintName(componentData.Namespace);
        spc.AddSource($"{matcherFileName}.g.cs", SourceText.From(matcherSource.WrapInNamespace(componentData.Namespace), Encoding.UTF8));
    }
    
    // Unique components are accessible directly from Context
    static string CreateComponentContextApiSource(
        in ComponentData componentData, 
        in ContextData contextData)
    {
        var source = componentData.Members.Length == 0 ?
            ComponentTemplates.GetFlagComponentContextApiSource(contextData, componentData) :
            ComponentTemplates.GetStandardComponentContextApiSource(contextData, componentData);

        return source + "\n";
    }

    static string GetPlainComponentApiHintName(
        in ComponentData componentData,
        in ContextData contextData)
    {
        var needsContextSegment = componentData.IsGenerated || componentData.ContextNames.Length > 1;
        var hintName = needsContextSegment
            ? contextData.ContextName + "." + componentData.ShortTypeName
            : componentData.ShortTypeName;

        return hintName.NamespacedHintName(componentData.Namespace);
    }

    // New components can be generated on the fly (like events)
    public static void GenerateExtraComponent(SourceProductionContext spc, 
        in ComponentData componentData)
    {
        var componentSource = ComponentTemplates.GeneratedComponentTemplate
            .Replace("${FullComponentName}", componentData.FullComponentName)
            .Replace("${Type}", componentData.FullTypeName);
        
        var fileName = componentData.FullComponentName.NamespacedHintName(componentData.Namespace);
        spc.AddSource(fileName + ".g.cs", SourceText.From(componentSource.WrapInNamespace(componentData.Namespace), Encoding.UTF8));
    }
}
