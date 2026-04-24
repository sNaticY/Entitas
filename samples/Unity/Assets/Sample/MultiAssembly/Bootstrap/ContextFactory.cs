using Entitas;
using Sample.MultiAssembly.FeatureA;
using Sample.MultiAssembly.FeatureB;

namespace Sample.MultiAssembly.Bootstrap
{
    public static class ContextFactory
    {
        public static ContextSchema CreateSchema()
        {
            return SharedContext.CreateSchemaBuilder()
                .AddPlayerAssembly()
                .AddHealthAssembly()
                .Build();
        }

        public static Contexts Create()
        {
            var schema = CreateSchema();
            var contexts = new Contexts()
                .RegisterShared(schema);

#if UNITY_EDITOR && !ENTITAS_DISABLE_VISUAL_DEBUGGING
            contexts.InitializeAllContextObservers();
#endif

            return contexts;
        }
    }
}
