using System;
using Entitas;
using Sample.MultiAssembly.FeatureA;
using Sample.MultiAssembly.FeatureB;
using UnityEngine;

namespace Sample.MultiAssembly.Bootstrap
{
    public sealed class MultiAssemblyController : MonoBehaviour
    {
        Systems _systems;
        SharedContext _sharedContext;


        void Awake()
        {
            var contexts = ContextFactory.Create();
            _sharedContext = contexts.GetShared();

            var player = _sharedContext.SetPlayer("Ada", 1);
            player.AddHealth(100);

            Debug.Log($"Created shared-context player '{_sharedContext.GetPlayer().Name}' from feature assemblies.");

            _systems = new Systems();

            _systems.Add(new SharedReactiveSystem(_sharedContext));
        }

        private void Start()
        {
            _systems.Initialize();
        }

        void Update()
        {
            _systems.Execute();
            _systems.Cleanup();

            var player = _sharedContext.GetPlayerEntity();
            player.ReplaceLevel(player.GetHealth().Value + 1);
        }

        private void OnDestroy()
        {
            _systems.TearDown();
        }
    }
}
