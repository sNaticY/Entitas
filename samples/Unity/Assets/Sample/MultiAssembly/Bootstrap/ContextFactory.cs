using Entitas;
using Sample.MultiAssembly.FeatureA;
using Sample.MultiAssembly.FeatureB;

namespace Sample.MultiAssembly.Bootstrap
{
    public static class ContextFactory
    {
        public static ContextSchema CreateSchema()
        {
            return new ContextSchemaBuilder("Shared")
                .AddPlayerFeature()
                .AddHealthFeature()
                .Build();
        }

        public static Contexts Create()
        {
            var schema = CreateSchema();
            var contexts = new Contexts()
                .Register(new SharedContext(schema));

            schema.InitializeEntityIndices(contexts);
            return contexts;
        }
    }
}
