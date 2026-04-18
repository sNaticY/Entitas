using System.Collections.Generic;
using Entitas;

public class SomeReactiveSystem : ReactiveSystem<GameEntity>
{
    public SomeReactiveSystem(GameContext context) : base(context) { }

    protected override ICollector<GameEntity> GetTrigger(IContext<GameEntity> context) =>
        context.CreateCollector(Matcher<GameEntity>.AllOf(0));

    protected override bool Filter(GameEntity entity) => true;

    protected override void Execute(List<GameEntity> entities) { }
}
