using Entitas;
using Entitas.Unity;
using UnityEngine;
using UnityEditor;

public class ReactiveSystemDestructorController : MonoBehaviour
{
    GameContext _gameContext;
    GameEntity _initialEntity;

    void Start()
    {
        _gameContext = SampleContexts.Create().GetGame();
        _gameContext.CreateContextObserver();
        new TestReactiveSystem(_gameContext);
        _initialEntity = _gameContext.CreateEntity();
        _initialEntity.SetTest(true);
        _initialEntity.Destroy();
    }

    void Update()
    {
        for (var i = 0; i < 5000; i++)
        {
            var e = _gameContext.CreateEntity();
            if (e == _initialEntity)
            {
                Debug.Log("Success: Reusing entity!");
                EditorApplication.isPlaying = false;
            }
        }
    }
}
