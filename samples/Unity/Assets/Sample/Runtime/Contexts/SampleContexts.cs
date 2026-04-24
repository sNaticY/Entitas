using Entitas;

public static class SampleContexts
{
    public static Contexts Create()
    {
        return new Contexts()
            .RegisterGame()
            .RegisterInput();
    }
}
