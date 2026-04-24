using Entitas;
using Entitas.Unity;
using Sample.MultiAssembly.FeatureA;

public sealed class PlayerFeature : Feature
{
    public PlayerFeature(Contexts contexts)
    {
        Add(new SharedReactiveSystem(contexts.GetShared()));
    }
}
