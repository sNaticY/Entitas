using System.Collections.Generic;
using Entitas;

public class SomeInitializeReactiveSystem : ReactiveSystem<GameEntity>, IInitializeSystem
{
    public SomeInitializeReactiveSystem(GameContext context) : base(context) { }

    protected override ICollector<GameEntity> GetTrigger(IContext<GameEntity> context) =>
        context.CreateCollector(GameMatcher.AllOf(0));

    protected override bool Filter(GameEntity entity) => true;

    public void Initialize() { }

    protected override void Execute(List<GameEntity> entities) { }
}
