using Entitas;

public static class SampleContexts
{
    public static Contexts Create()
    {
        return new Contexts()
            .Register(new GameContext())
            .Register(new InputContext());
    }
}
