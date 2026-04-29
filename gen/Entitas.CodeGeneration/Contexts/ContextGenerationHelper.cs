using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Entitas.CodeGeneration.Components;
using Entitas.CodeGeneration.Contexts.Data;
using Entitas.CodeGeneration.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Entitas.CodeGeneration.Contexts;

public static class ContextGenerationHelper
{
    public const string DefaultContextName = "Game";

    public const string ContextAttributeName = "ContextAttribute";
    public const string ContextAttributeTypeName = "Entitas.CodeGeneration.Attributes.ContextAttribute";

    const string AttributeSuffix = "Attribute";
    
    public static IncrementalValueProvider<ImmutableArray<ContextData>> GetContextsData(IncrementalGeneratorInitializationContext context)
    {
        var contextsData = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsContextCandidateSyntax(s),
                transform: static (ctx, _) => TryGetContextData(ctx))
            .Where(static c => c is not null)
            .Select(static (c, _) => (ContextData)c!) // null-forgiving operator, safe after filtering
            .Collect();
        
        return contextsData;
    }
    
    static bool IsContextCandidateSyntax(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax c || c.BaseList == null)
            return false;

        if (!c.Identifier.Text.EndsWith(AttributeSuffix))
            return false;
        
        foreach (var baseTypeSyntax in c.BaseList.Types)
        {
            var baseTypeName = baseTypeSyntax.Type switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,
                QualifiedNameSyntax q => q.Right.Identifier.Text,
                _ => null,
            };
            
            if (baseTypeName == ContextAttributeName)
                return true;
        }
        
        return false;
    }
    
    static ContextData? TryGetContextData(GeneratorSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;

        if (ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classSyntax) is not INamedTypeSymbol classTypeSymbol)
            return null;

        if (classTypeSymbol.IsAbstract || classTypeSymbol.BaseType?.ToDisplayString() != ContextAttributeTypeName)
            return null;

        var ctor = classTypeSymbol.InstanceConstructors.FirstOrDefault(constructor =>
            constructor.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax())
                .OfType<ConstructorDeclarationSyntax>()
                .Any(syntax => syntax.Initializer?.Kind() == Microsoft.CodeAnalysis.CSharp.SyntaxKind.BaseConstructorInitializer));

        var ctorSyntax = ctor?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as ConstructorDeclarationSyntax;
        var baseArg = ctorSyntax?.Initializer?.ArgumentList.Arguments.FirstOrDefault()?.Expression;

        if (baseArg is not null)
        {
            var value = context.SemanticModel.GetConstantValue(baseArg);
            if (value.HasValue && value.Value is string contextName)
                return new ContextData(contextName);
        }

        var fallbackContextName = classSyntax.Identifier.Text.RemoveLast(AttributeSuffix);
        return new ContextData(fallbackContextName);
    }
    
    public static void GenerateContexts(
        SourceProductionContext spc,
        ImmutableArray<ContextData> contextsData,
        in EntitasGeneratorOptions options)
    {
        foreach (var contextData in contextsData)
        {
            if (options.ContextGenerationEnabled)
                GenerateContext(spc, contextData, options);

            if (options.ContextMatcherGenerationEnabled)
                GenerateContextMatcher(spc, contextData, includeStaticMembers: true, dataHintName: contextData.MatcherTypeName);
            else if (options.ComponentMatcherGenerationEnabled)
                GenerateContextMatcher(spc, contextData, includeStaticMembers: false, dataHintName: contextData.MatcherTypeName + "Receiver");

            if (options.ContextEntityGenerationEnabled)
                GenerateContextEntity(spc, contextData);

            if (options.ComponentContextExtensionGenerationEnabled)
                GenerateContextsExtension(spc, contextData);
        }
    }
    
    public static void GenerateContext(SourceProductionContext spc, ContextData data, in EntitasGeneratorOptions options)
    {
        var matcherProperty = options.ContextMatcherGenerationEnabled || options.ComponentMatcherGenerationEnabled
            ? ContextTemplates.ContextMatcherPropertyTemplate
                .Replace("${MatcherType}", data.MatcherTypeName)
            : string.Empty;

        var generatedSource = ContextTemplates.ContextTemplate
            .Replace("${ContextName}", data.ContextName)
            .Replace("${ContextType}", data.ContextTypeName)
            .Replace("${EntityType}", data.EntityTypeName)
            .Replace("${MatcherProperty}", matcherProperty)
            .Replace("${Lookup}", data.ContextName + ComponentGenerationHelper.ComponentsLookupName);
        
        spc.AddSource(data.ContextTypeName + ".g.cs", SourceText.From(generatedSource, Encoding.UTF8));
    }

    public static void GenerateContextMatcher(
        SourceProductionContext spc,
        ContextData data,
        bool includeStaticMembers,
        string dataHintName)
    {
        var staticMembers = includeStaticMembers
            ? ContextTemplates.ContextMatcherStaticMembersTemplate
                .Replace("${EntityType}", data.EntityTypeName)
            : string.Empty;

        var generatedSource = ContextTemplates.ContextMatcherTemplate
            .Replace("${MatcherType}", data.MatcherTypeName)
            .Replace("${StaticMatcherMembers}", staticMembers)
            .Replace("${EntityType}", data.EntityTypeName);
             
        spc.AddSource(dataHintName + ".g.cs", SourceText.From(generatedSource, Encoding.UTF8));
    }

    public static void GenerateContextEntity(SourceProductionContext spc, ContextData data)
    {
        var source = ContextTemplates.ContextEntityTemplate
            .Replace("${EntityType}", data.EntityTypeName);
        
        spc.AddSource(data.EntityTypeName + ".g.cs", SourceText.From(source, Encoding.UTF8));
    }

    public static void GenerateContextsExtension(SourceProductionContext spc, ContextData data)
    {
        var source = ContextTemplates.ContextsExtensionTemplate
            .Replace("${ContextName}", data.ContextName)
            .Replace("${ContextType}", data.ContextTypeName);

        spc.AddSource(data.ContextName + "ContextsExtension.g.cs", SourceText.From(source, Encoding.UTF8));
    }
}
