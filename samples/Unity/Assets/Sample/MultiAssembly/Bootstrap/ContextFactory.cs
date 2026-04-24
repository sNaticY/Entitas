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
            return new Contexts()
                .Register(new SharedContext(schema), schema);
        }
    }
}
