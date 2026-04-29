using System.Collections.Generic;
using Entitas;

public class ProcessRandomValueSystem : ReactiveSystem<GameEntity>
{
    public ProcessRandomValueSystem(GameContext context) : base(context) { }

    protected override ICollector<GameEntity> GetTrigger(IContext<GameEntity> context) =>
        context.CreateCollector(GameMatcher.Instance.MyFloat());

    protected override bool Filter(GameEntity entity) => true;

    protected override void Execute(List<GameEntity> entities)
    {
        foreach (var entity in entities)
            entity.Destroy();
    }
}
