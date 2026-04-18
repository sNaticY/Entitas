using Entitas;
using Entitas.Unity;
using UnityEngine;

public class SystemsController : MonoBehaviour
{
    GameContext _gameContext;
    Systems _systems;

    void Start()
    {
        _gameContext = SampleContexts.Create().GetGame();
        _gameContext.CreateContextObserver();

        _systems = CreateNestedSystems();
        //// Test calls
        _systems.Initialize();
        _systems.Execute();
        _systems.Cleanup();
        _systems.TearDown();

        _gameContext.CreateEntity().AddMyString("");
    }

    Systems CreateNestedSystems()
    {
        var systems1 = new Systems();
        var systems2 = new Systems();
        var systems3 = new Systems();

        systems1.Add(systems2);
        systems2.Add(systems3);
        systems1.Add(CreateSomeSystems());

        return new Systems()
            .Add(systems1);
    }

    Systems CreateSomeSystems()
    {
        return new SomeSystems(_gameContext);
    }

    void Update()
    {
        _gameContext.GetGroup(GameMatcher.MyString()).GetSingleEntity()
            .ReplaceMyString(Random.value.ToString());

        _systems.Execute();
        _systems.Cleanup();
    }

    Systems CreateAllSystemCombinations()
    {
        return new Systems()
            .Add(new SomeInitializeSystem())
            .Add(new SomeExecuteSystem())
            .Add(new SomeReactiveSystem(_gameContext))
            .Add(new SomeInitializeExecuteSystem())
            .Add(new SomeInitializeReactiveSystem(_gameContext));
    }

    Systems CreateSubSystems()
    {
        var allSystems = CreateAllSystemCombinations();
        var subSystems = new Systems().Add(allSystems);
        return new Systems()
            .Add(allSystems)
            .Add(allSystems)
            .Add(subSystems)
            .Add(subSystems);
    }

    Systems CreateSameInstance()
    {
        var system = new RandomDurationSystem();
        return new Systems()
            .Add(system)
            .Add(system)
            .Add(system);
    }

    Systems CreateEmptySystems()
    {
        var systems1 = new Systems();
        var systems2 = new Systems();
        var systems3 = new Systems();

        systems1.Add(systems2);
        systems2.Add(systems3);

        return new Systems()
            .Add(systems1);
    }

    sealed class SomeSystems : Systems
    {
        public SomeSystems(GameContext gameContext)
        {
            Add(new SlowInitializeSystem());
            Add(new SlowInitializeExecuteSystem());
            Add(new FastSystem());
            Add(new SlowSystem());
            Add(new RandomDurationSystem());
            Add(new AReactiveSystem(gameContext));

            Add(new RandomValueSystem(gameContext));
            Add(new ProcessRandomValueSystem(gameContext));
            Add(new CleanupSystem());
            Add(new TearDownSystem());
            Add(new MixedSystem());
        }
    }
}
