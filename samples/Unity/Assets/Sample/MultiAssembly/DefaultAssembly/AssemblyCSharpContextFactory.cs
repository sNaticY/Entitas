using Entitas;
using Sample.MultiAssembly.FeatureA;
using Sample.MultiAssembly.FeatureB;

public static class AssemblyCSharpContextFactory
{
    public static ContextSchema CreateSchema()
    {
        var builder = SharedContext.CreateSchemaBuilder()
            .AddPlayerAssembly()
            .AddHealthAssembly();

        builder = SharedAssemblyCSharpAssemblySchemaExtensions.AddAssemblyCSharpAssembly(builder);
        return builder.Build();
    }

    public static Contexts Create() => Create(CreateSchema());

    public static Contexts Create(ContextSchema schema)
    {
        var contexts = new Contexts()
            .RegisterShared(schema);

#if UNITY_EDITOR && !ENTITAS_DISABLE_VISUAL_DEBUGGING
        contexts.InitializeAllContextObservers();
#endif

        return contexts;
    }
}
