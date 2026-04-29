using System.Collections.Generic;
using Entitas;
using Entitas.Unity;
using UnityEngine;

public class GroupAllocController : MonoBehaviour
{
    public GroupAlloc Mode;
    public int Count;

    readonly List<GameEntity> _buffer = new List<GameEntity>();
    GameContext _gameContext;
    IGroup<GameEntity> _group;

    void Start()
    {
        _gameContext = SampleContexts.Create().GetGame();
        _gameContext.CreateContextObserver();
        _group = _gameContext.GetGroup(_gameContext.Matcher.MyInt());
    }

    void Update()
    {
        _gameContext.DestroyAllEntities();
        for (var i = 0; i < Count; i++)
            _gameContext.CreateEntity().AddMyInt(i);

        Iterate();
    }

    void Iterate()
    {
        switch (Mode)
        {
            case GroupAlloc.Group:
                foreach (var entity in _group)
                {
                    var unused = entity.GetMyInt().Value;
                }

                break;
            case GroupAlloc.GetEntities:
                foreach (var entity in _group.GetEntities())
                {
                    var unused = entity.GetMyInt().Value;
                }

                break;
            case GroupAlloc.GetEntitiesBuffer:
                foreach (var entity in _group.GetEntities(_buffer))
                {
                    var unused = entity.GetMyInt().Value;
                }

                break;
        }
    }
}

public enum GroupAlloc
{
    Group,
    GetEntities,
    GetEntitiesBuffer
}
