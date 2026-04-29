using System.Collections.Generic;
using Entitas;

public class TestReactiveSystem : ReactiveSystem<GameEntity>
{
    public TestReactiveSystem(GameContext context) : base(context) { }

    protected override ICollector<GameEntity> GetTrigger(IContext<GameEntity> context) =>
        context.CreateCollector(GameMatcher.Instance.Test());

    protected override bool Filter(GameEntity entity) => true;

    protected override void Execute(List<GameEntity> entities) { }
}
