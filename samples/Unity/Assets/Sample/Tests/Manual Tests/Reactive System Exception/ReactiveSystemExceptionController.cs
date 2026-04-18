using System;
using System.Collections.Generic;
using System.Globalization;
using Entitas;
using Entitas.Unity;
using UnityEngine;
using Random = UnityEngine.Random;

public class ReactiveSystemExceptionController : MonoBehaviour
{
    GameEntity _entity;
    ExceptionReactiveSystem _system;

    void Start()
    {
        var gameContext = SampleContexts.Create().GetGame();
        gameContext.CreateContextObserver();
        _entity = gameContext.CreateEntity();
        _system = new ExceptionReactiveSystem(gameContext);
    }

    void Update()
    {
        _entity.ReplaceMyString(Random.value.ToString(CultureInfo.InvariantCulture));
        _system.Execute();
    }
}

public class ExceptionReactiveSystem : ReactiveSystem<GameEntity>
{
    public ExceptionReactiveSystem(GameContext context) : base(context) { }

    protected override ICollector<GameEntity> GetTrigger(IContext<GameEntity> context) =>
        context.CreateCollector(GameMatcher.MyString());

    protected override bool Filter(GameEntity entity) => true;

    protected override void Execute(List<GameEntity> entities)
    {
        if (Random.value > 0.99f)
            throw new Exception("ExceptionReactiveSystem Exception!");
    }
}
