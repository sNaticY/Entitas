namespace Entitas.Generators.IntegrationTests;

static class TestContexts
{
    public static Contexts Create()
    {
        var contexts = new Contexts()
            .Register(new MainContext())
            .Register(new ConfigContext());

        contexts.InitializeMainEntityIndices();
        contexts.InitializeConfigEntityIndices();
        return contexts;
    }
}
