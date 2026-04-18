using UnityEngine;
using Entitas;
using Entitas.Unity;
using UnityEditor;

public class CollectorDestructorController : MonoBehaviour
{
    GameContext _gameContext;
    GameEntity _initialEntity;

    void Start()
    {
        _gameContext = SampleContexts.Create().GetGame();
        _gameContext.CreateContextObserver();
        _gameContext.GetGroup(GameMatcher.Test()).CreateCollector();
        _initialEntity = _gameContext.CreateEntity();
        _initialEntity.SetTest(true);
        _initialEntity.Destroy();
        // TODO
        // context.ClearGroups();
    }

    void Update()
    {
        for (var i = 0; i < 5000; i++)
        {
            var entity = _gameContext.CreateEntity();
            if (entity == _initialEntity)
            {
                Debug.Log("Reusing entity!");
                EditorApplication.isPlaying = false;
            }
        }
    }
}
