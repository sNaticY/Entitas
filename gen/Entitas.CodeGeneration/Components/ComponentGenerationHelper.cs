using System.Collections.Immutable;
using System.Text;
using Entitas.CodeGeneration.Components.Data;
using Entitas.CodeGeneration.Components.Extensions;
using Entitas.CodeGeneration.Contexts.Data;
using Entitas.CodeGeneration.Events;
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
    
    public static IncrementalValueProvider<ImmutableArray<ComponentData>> GetComponentsData(IncrementalGeneratorInitializationContext context)
    {
        var declaredComponentsData = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsComponentCandidateSyntax(s),
                transform: static (ctx, _) => TryGetComponentType(ctx))
            .Where(static c => c is not null)
            .Select(static (c, _) => (ComponentData)c!) // null-forgiving operator, safe after filtering
            .Collect();

        // Include generated components (ex: events)
        return MergeWithGeneratedComponents(declaredComponentsData);
    }

    static IncrementalValueProvider<ImmutableArray<ComponentData>> MergeWithGeneratedComponents(
        IncrementalValueProvider<ImmutableArray<ComponentData>> declaredComponentsData)
    {
        return declaredComponentsData.Select(static (originalComponents, _) => 
        {
            var builder = ImmutableArray.CreateBuilder<ComponentData>(originalComponents.Length);
            builder.AddRange(originalComponents);

            // Add generated components (ex: events)
            foreach (var component in originalComponents)
            {
                if (!component.HasEvents) continue;

                EventsGenerationHelper.CreateEventComponents(component, builder);
            }

            return builder.ToImmutable();
        });
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
    
    public static void GenerateEntityComponent(SourceProductionContext spc, 
        in ComponentData componentData, 
        in ContextData contextData)
    {
        var source = string.Empty;
        if (componentData.IsUnique)
            source += CreateComponentContextApiSource(componentData, contextData);
        
        if (componentData.Members.Length == 0)
        {
            source += ComponentTemplates.GetFlagComponentEntityApiSource(contextData, componentData);
        }
        else
        {
            source += ComponentTemplates.GetStandardComponentEntityApiSource(contextData, componentData);
        }
        
        source += "\n" + ComponentTemplates.GetComponentMatcherApiSource(contextData, componentData);

        var fileName = contextData.ContextName + componentData.GetComponentName().AddComponentSuffix();
        spc.AddSource($"{fileName}.g.cs", SourceText.From(source, Encoding.UTF8));
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
    
    // New components can be generated on the fly (like events)
    public static void GenerateExtraComponent(SourceProductionContext spc, 
        in ComponentData componentData)
    {
        var componentSource = ComponentTemplates.GeneratedComponentTemplate
            .Replace("${FullComponentName}", componentData.FullComponentName)
            .Replace("${Type}", componentData.FullTypeName);
        
        spc.AddSource(componentData.FullComponentName + ".g.cs", SourceText.From(componentSource, Encoding.UTF8));
    }
}
