namespace Entitas.CodeGeneration.Contexts;

public static class ContextTemplates
{
    public const string ContextsTemplate =
        @"public partial class Contexts
{
    public static Contexts sharedInstance
    {
        get
        {
            if (_sharedInstance == null)
            {
                _sharedInstance = new Contexts();
            }

            return _sharedInstance;
        }
        set { _sharedInstance = value; }
    }

    static Contexts _sharedInstance;

${contextPropertyList}

    public global::Entitas.IContext[] allContexts { get { return new global::Entitas.IContext [] { ${contextList} }; } }

    public Contexts()
    {
${contextAssignmentList}

        var postConstructors = System.Linq.Enumerable.Where(
            GetType().GetMethods(),
            method => System.Attribute.IsDefined(method, typeof(global::Entitas.CodeGeneration.Attributes.PostConstructorAttribute))
        );

        foreach (var postConstructor in postConstructors)
        {
            postConstructor.Invoke(this, null);
        }
    }

    public void Reset()
    {
        var contexts = allContexts;
        for (int i = 0; i < contexts.Length; i++)
        {
            contexts[i].Reset();
        }
    }
}
";
    
    public const string ContextPropertyTemplate = @"    public ${ContextType} ${contextName} { get; set; }";
    public const string ContextListTemplate = @"${contextName}";
    public const string ContextAssignmentTemplate = @"        ${contextName} = new ${ContextType}();";
    
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
