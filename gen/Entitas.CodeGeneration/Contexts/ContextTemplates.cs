namespace Entitas.CodeGeneration.Contexts;

public static class ContextTemplates
{
    public const string ContextsExtensionTemplate =
        @"namespace Entitas
{
public static class ${ContextName}ContextsExtension
{
    public static ${ContextType} Get${ContextName}(this global::Entitas.Contexts contexts)
    {
        return contexts.Get<${ContextType}>();
    }
}
}
";
    
    public const string ContextTemplate =
        @"public sealed partial class ${ContextType} : global::Entitas.Context<${EntityType}>
{
    public ${ContextType}()
        : base(
            ${Lookup}.TotalComponents,
            0,
            new global::Entitas.ContextInfo(
                ""${ContextName}"",
                ${Lookup}.componentNames,
                ${Lookup}.componentTypes
            ),
            (entity) =>

#if (ENTITAS_FAST_AND_UNSAFE)
                new global::Entitas.UnsafeAERC(),
#else
                new global::Entitas.SafeAERC(entity),
#endif
            () => new ${EntityType}()
        ) 
    {
    }
}
";
    
    public const string ContextMatcherTemplate =
        @"public sealed partial class ${MatcherType} 
{
    public static global::Entitas.IAllOfMatcher<${EntityType}> AllOf(params int[] indices) 
    {
        return global::Entitas.Matcher<${EntityType}>.AllOf(indices);
    }

    public static global::Entitas.IAllOfMatcher<${EntityType}> AllOf(params global::Entitas.IMatcher<${EntityType}>[] matchers)
    {
        return global::Entitas.Matcher<${EntityType}>.AllOf(matchers);
    }

    public static global::Entitas.IAnyOfMatcher<${EntityType}> AnyOf(params int[] indices)
    {
        return global::Entitas.Matcher<${EntityType}>.AnyOf(indices);
    }

    public static global::Entitas.IAnyOfMatcher<${EntityType}> AnyOf(params global::Entitas.IMatcher<${EntityType}>[] matchers)
    {
        return global::Entitas.Matcher<${EntityType}>.AnyOf(matchers);
    }
}
";
    
    public const string ContextEntityTemplate =
        @"public sealed partial class ${EntityType} : global::Entitas.Entity
{
}
";
}
