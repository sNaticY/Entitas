using Entitas.CodeGeneration.Components.Data;
using Entitas.CodeGeneration.Components;
using Entitas.CodeGeneration.Components.Extensions;
using Entitas.CodeGeneration.Contexts.Data;
using Entitas.CodeGeneration.EntityIndex.Extensions;
using Entitas.CodeGeneration.Extensions;

namespace Entitas.CodeGeneration.EntityIndex;

public static class EntityIndexTemplates
{
    public const string EntityIndexContextsTemplate =
        @"namespace Entitas
{
public static class ${ContextName}EntityIndices
{
${indexConstants}
}

public static class ${ContextName}ContextsEntityIndexExtension
{
    public static void Initialize${ContextName}EntityIndices(this global::Entitas.Contexts contexts)
    {
        var ${contextName} = contexts.Get${ContextName}();
${addIndices}
    }
}

public static class ${ContextName}EntityIndicesExtension
{
${getIndices}
}
}";

    public const string IndexConstantTemplate = @"    public const string ${IndexName} = ""${IndexName}"";";

    const string AddIndexTemplate =
        @"        ${contextName}.AddEntityIndex(new ${IndexType}<${ContextName}Entity, ${KeyType}>(
            ${ContextName}EntityIndices.${IndexName},
            ${contextName}.GetGroup(global::Entitas.Matcher<${ContextName}Entity>.AllOf(${ComponentHandle})),
            (e, c) => ((${ComponentType})c).${MemberName}));";
    
    public static string GetAddIndexSource(
        string indexName,
        in ContextData contextData,
        in ComponentData componentData,
        in MemberData memberData)
    {
        var contextName = contextData.ContextName;
        var contextNameLower = contextName.ToLowerFirst();
        var indexType = memberData.GetEntityIndexType();
        var componentHandle = ComponentTemplates.GetComponentHandleExpression(contextData, componentData);
        
        return AddIndexTemplate
            .Replace("${ContextName}", contextName)
            .Replace("${contextName}", contextNameLower)
            .Replace("${ComponentType}", componentData.FullTypeName)
            .Replace("${IndexName}", indexName)
            .Replace("${MemberName}", memberData.Name)
            .Replace("${KeyType}", memberData.Type)
            .Replace("${IndexType}", indexType)
            .Replace("${ComponentHandle}", componentHandle);
    }

    // const string ADD_CUSTOM_INDEX_TEMPLATE =
    //     @"        ${contextName}.AddEntityIndex(new ${IndexType}(${contextName}));";

    const string GetIndexTemplate =
        @"    public static System.Collections.Generic.HashSet<${ContextName}Entity> GetEntitiesWith${IndexName}(this ${ContextName}Context context, ${KeyType} ${MemberName}) {
        return ((${IndexType}<${ContextName}Entity, ${KeyType}>)context.GetEntityIndex(${ContextName}EntityIndices.${IndexName})).GetEntities(${MemberName});
    }";
    
    public static string GetIndexSource(
        string indexName,
        in ContextData contextData,
        in MemberData memberData)
    {
        return GetIndexTemplate
            .Replace("${ContextName}", contextData.ContextName)
            .Replace("${IndexName}", indexName)
            .Replace("${MemberName}", memberData.Name)
            .Replace("${KeyType}", memberData.Type)
            .Replace("${IndexType}", memberData.GetEntityIndexType());
    }

    const string GetPrimaryIndexTemplate =
        @"    public static ${ContextName}Entity GetEntityWith${IndexName}(this ${ContextName}Context context, ${KeyType} ${MemberName}) {
        return ((${IndexType}<${ContextName}Entity, ${KeyType}>)context.GetEntityIndex(${ContextName}EntityIndices.${IndexName})).GetEntity(${MemberName});
    }";
    
    public static string GetPrimaryIndexSource(
        string indexName,
        in ContextData contextData,
        in MemberData memberData)
    {
        return GetPrimaryIndexTemplate
            .Replace("${ContextName}", contextData.ContextName)
            .Replace("${IndexName}", indexName)
            .Replace("${MemberName}", memberData.Name)
            .Replace("${KeyType}", memberData.Type)
            .Replace("${IndexType}", memberData.GetEntityIndexType());
    }  

    const string ComponentEntityIndicesTemplate =
        @"public static class ${IndexConstantsType}
{
${indexConstants}
}

public static class ${IndexSchemaExtensionsType}
{
    public static global::Entitas.ContextSchemaBuilder Add${IndexConstantsType}(this global::Entitas.ContextSchemaBuilder builder)
    {
${indexRegistrations}
        return builder;
    }
}

public static class ${IndexExtensionsType}
{
${getIndices}
}
";

    const string ComponentIndexRegistrationTemplate =
        @"        builder.AddEntityIndex(${IndexConstantsType}.${IndexName}, contexts =>
        {
            var context = global::Entitas.${ContextName}ContextsExtension.Get${ContextName}(contexts);
            context.AddEntityIndex(new ${IndexType}<${ContextName}Entity, ${KeyType}>(
                ${IndexConstantsType}.${IndexName},
                context.GetGroup(global::Entitas.Matcher<${ContextName}Entity>.AllOf(${ComponentHandle})),
                (e, c) => ((${ComponentType})c).${MemberName}));
        });";

    public static string GetComponentEntityIndexSource(
        in ContextData contextData,
        in ComponentData componentData)
    {
        var indexConstantsBuilder = new System.Text.StringBuilder();
        var indexRegistrationsBuilder = new System.Text.StringBuilder();
        var getIndicesBuilder = new System.Text.StringBuilder();
        var indexConstantsType = contextData.ContextName + componentData.GetScopedComponentName() + "EntityIndices";
        var componentHandle = ComponentTemplates.GetComponentHandleExpression(contextData, componentData);
        var hasMultipleIndices = componentData.GetEntityIndexCount() > 1;

        foreach (var memberData in componentData.Members)
        {
            if (!memberData.IsEntityIndex)
                continue;

            var indexName = hasMultipleIndices
                ? componentData.FullComponentName + memberData.Name.ToUpperFirst()
                : componentData.FullComponentName;
            var apiIndexName = hasMultipleIndices
                ? componentData.GetScopedComponentName() + memberData.Name.ToUpperFirst()
                : componentData.GetScopedComponentName();

            indexConstantsBuilder.AppendLine(IndexConstantTemplate.Replace("${IndexName}", indexName));
            indexRegistrationsBuilder.AppendLine(ComponentIndexRegistrationTemplate
                .Replace("${IndexConstantsType}", indexConstantsType)
                .Replace("${IndexName}", indexName)
                .Replace("${ContextName}", contextData.ContextName)
                .Replace("${IndexType}", memberData.GetEntityIndexType())
                .Replace("${KeyType}", memberData.Type)
                .Replace("${ComponentHandle}", componentHandle)
                .Replace("${ComponentType}", componentData.FullTypeName)
                .Replace("${MemberName}", memberData.Name));

            var getIndexSource = memberData.EntityIndexType switch
            {
                EntityIndexType.PrimaryEntityIndex => GetComponentPrimaryIndexSource(indexConstantsType, indexName, apiIndexName, contextData, memberData),
                EntityIndexType.EntityIndex => GetComponentIndexSource(indexConstantsType, indexName, apiIndexName, contextData, memberData),
                _ => string.Empty,
            };
            getIndicesBuilder.Append(getIndexSource + "\n\n");
        }

        return ComponentEntityIndicesTemplate
            .Replace("${IndexConstantsType}", indexConstantsType)
            .Replace("${IndexSchemaExtensionsType}", indexConstantsType + "SchemaExtensions")
            .Replace("${IndexExtensionsType}", indexConstantsType + "Extensions")
            .Replace("${indexConstants}", indexConstantsBuilder.ToString().RemoveLast("\n"))
            .Replace("${indexRegistrations}", indexRegistrationsBuilder.ToString().RemoveLast("\n"))
            .Replace("${getIndices}", getIndicesBuilder.ToString().RemoveLast("\n\n"));
    }

    static string GetComponentIndexSource(
        string indexConstantsType,
        string indexName,
        string apiIndexName,
        in ContextData contextData,
        in MemberData memberData) =>
        GetIndexTemplate
            .Replace("${ContextName}EntityIndices", indexConstantsType)
            .Replace("${ContextName}", contextData.ContextName)
            .Replace("${IndexName}", apiIndexName)
            .Replace(indexConstantsType + "." + apiIndexName, indexConstantsType + "." + indexName)
            .Replace("${MemberName}", memberData.Name)
            .Replace("${KeyType}", memberData.Type)
            .Replace("${IndexType}", memberData.GetEntityIndexType());

    static string GetComponentPrimaryIndexSource(
        string indexConstantsType,
        string indexName,
        string apiIndexName,
        in ContextData contextData,
        in MemberData memberData) =>
        GetPrimaryIndexTemplate
            .Replace("${ContextName}EntityIndices", indexConstantsType)
            .Replace("${ContextName}", contextData.ContextName)
            .Replace("${IndexName}", apiIndexName)
            .Replace(indexConstantsType + "." + apiIndexName, indexConstantsType + "." + indexName)
            .Replace("${MemberName}", memberData.Name)
            .Replace("${KeyType}", memberData.Type)
            .Replace("${IndexType}", memberData.GetEntityIndexType());

//     const string CUSTOM_METHOD_TEMPLATE =
//         @"    public static ${ReturnType} ${MethodName}(this ${ContextName}Context context, ${methodArgs}) {
//         return ((${IndexType})(context.GetEntityIndex(Contexts.${IndexName}))).${MethodName}(${args});
//     }
// ";
}
