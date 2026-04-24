using Entitas;
using Sample.MultiAssembly.FeatureA;
using Sample.MultiAssembly.FeatureB;
using UnityEngine;

namespace Sample.MultiAssembly.Bootstrap
{
    public sealed class MultiAssemblyController : MonoBehaviour
    {
        SharedContext _sharedContext;

        void Start()
        {
            var contexts = MultiAssemblyContextFactory.Create();
            _sharedContext = contexts.GetShared();

            var player = _sharedContext.SetPlayer("Ada", 1);
            player.AddHealth(100);

            Debug.Log($"Created shared-context player '{_sharedContext.GetPlayer().Name}' from feature assemblies.");
        }

        void Update()
        {
            var player = _sharedContext.GetPlayerEntity();
            if (player == null || !player.HasHealth())
                return;

            player.ReplaceHealth(player.GetHealth().Value + 1);
        }
    }
}
