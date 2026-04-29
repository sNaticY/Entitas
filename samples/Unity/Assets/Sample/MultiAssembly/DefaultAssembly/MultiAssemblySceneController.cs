using System.Collections.Generic;
using System.Text;
using Entitas;
using Sample.MultiAssembly.FeatureA;
using Sample.MultiAssembly.FeatureB;
using UnityEngine;

public sealed class MultiAssemblySceneController : MonoBehaviour, global::IAnyManaListener
{
    Contexts _contexts;
    SharedContext _sharedContext;
    SharedEntity _player;
    SharedEntity _session;
    SharedEntity _expiredEntity;
    SharedEntity _destroyedEntity;
    Systems _systems;
    IGroup<SharedEntity> _manaGroup;
    ICollector<SharedEntity> _manaCollector;
    readonly StringBuilder _status = new StringBuilder();
    int _frame;
    int _manaEvents;
    int _collectorHits;
    int _lastManaEvent;

    void Awake()
    {
        var schema = AssemblyCSharpContextFactory.CreateSchema();
        _contexts = AssemblyCSharpContextFactory.Create(schema);
        _sharedContext = _contexts.GetShared();

        _manaGroup = _sharedContext.GetGroup(_sharedContext.Matcher.Mana());
        _manaCollector = _sharedContext.CreateCollector(_sharedContext.Matcher.Mana().Added());
        _systems = new Systems()
            .Add(schema.CreateEventSystems(_contexts))
            .Add(new SharedReactiveSystem(_sharedContext))
            .Add(schema.CreateCleanupSystems(_contexts));

        _sharedContext.CreateEntity().AddAnyManaListener(this);

        _player = _sharedContext.SetPlayer("Ada", 1);
        _player.AddHealth(100);
        _player.AddMana(10);
        _player.AddLevel(1);

        _session = _sharedContext.SetSession("play-mode");
        _expiredEntity = _sharedContext.CreateEntity();
        _expiredEntity.SetExpired(true);
        _destroyedEntity = _sharedContext.CreateEntity();
        _destroyedEntity.SetDestroyed(true);

        Debug.Log("Created one Shared context from Root asmdef + FeatureA asmdef + FeatureB asmdef + Assembly-CSharp.");
    }

    void Start()
    {
        _systems.Initialize();
        RefreshStatus();
    }

    void Update()
    {
        _frame++;
        if (_frame % 60 == 0)
        {
            _player.ReplaceMana(_player.GetMana().Value + 1);
            _player.ReplaceLevel(_player.GetLevel().Value + 1);

            _expiredEntity = _sharedContext.CreateEntity();
            _expiredEntity.SetExpired(true);
            _destroyedEntity = _sharedContext.CreateEntity();
            _destroyedEntity.SetDestroyed(true);
        }

        _systems.Execute();
        _systems.Cleanup();

        _collectorHits += _manaCollector.Count;
        _manaCollector.ClearCollectedEntities();
        RefreshStatus();
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 620, 260), GUI.skin.box);
        GUILayout.Label("Entitas multi-assembly sample: one Shared context");
        GUILayout.Label(_status.ToString());
        GUILayout.EndArea();
    }

    void OnDestroy()
    {
        _manaCollector?.Deactivate();
        _systems?.TearDown();
    }

    public void OnAnyMana(SharedEntity entity, int value)
    {
        _manaEvents++;
        _lastManaEvent = value;
    }

    void RefreshStatus()
    {
        var mana = _player.HasMana() ? _player.GetMana().Value : 0;
        var playerLevel = _sharedContext.GetPlayer().Level;
        var levelComponent = _player.HasLevel() ? _player.GetLevel().Value : 0;
        var health = _player.HasHealth() ? _player.GetHealth().Value : 0;
        var playerIndexOk = ReferenceEquals(_sharedContext.GetEntityWithPlayerName("Ada"), _player);
        var sessionIndexOk = ReferenceEquals(_sharedContext.GetEntityWithSessionId("play-mode"), _session);
        var manaIndexOk = Contains(_sharedContext.GetEntitiesWithManaValue(mana), _player);
        var cleanupRemoveOk = _expiredEntity != null && _expiredEntity.IsEnabled && !_expiredEntity.IsExpired();
        var cleanupDestroyOk = _destroyedEntity != null && !_destroyedEntity.IsEnabled;

        _status.Length = 0;
        _status.AppendLine("Assemblies: Entitas.Sample.MultiAssembly.Root, FeatureA, FeatureB, Assembly-CSharp");
        _status.AppendLine($"Player unique/index: Ada playerLevel={playerLevel} levelComponent={levelComponent} health={health} ok={playerIndexOk}");
        _status.AppendLine($"Assembly-CSharp unique/index: session={_session.GetSession().Id} ok={sessionIndexOk}");
        _status.AppendLine($"Assembly-CSharp event/index: mana={mana} events={_manaEvents} last={_lastManaEvent} manaIndexOk={manaIndexOk}");
        _status.AppendLine($"Matcher/group/collector: manaGroup={_manaGroup.Count} collectorHits={_collectorHits}");
        _status.AppendLine($"Cleanup systems: removeComponent={cleanupRemoveOk} destroyEntity={cleanupDestroyOk}");
        _status.AppendLine("Generated systems run every frame; mana/level update every 60 frames.");
    }

    static bool Contains(IEnumerable<SharedEntity> entities, SharedEntity target)
    {
        foreach (var entity in entities)
        {
            if (ReferenceEquals(entity, target))
                return true;
        }

        return false;
    }
}
