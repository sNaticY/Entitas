namespace Entitas.Generators.IntegrationTests;

static class TestContexts
{
    public static Contexts Create()
    {
        return new Contexts()
            .RegisterMain()
            .RegisterConfig();
    }
}
