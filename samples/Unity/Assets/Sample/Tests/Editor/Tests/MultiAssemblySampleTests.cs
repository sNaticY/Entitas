using System.Collections.Generic;
using Entitas;
using NUnit.Framework;
using Sample.MultiAssembly.Bootstrap;
using Sample.MultiAssembly.FeatureA;
using Sample.MultiAssembly.FeatureB;

public sealed class MultiAssemblySampleTests
{
    [Test]
    public void CreatesSharedContextFromFeatureAssemblies()
    {
        var contexts = ContextFactory.Create();
        var shared = contexts.GetShared();

        var player = shared.SetPlayer("Ada", 1);
        player.AddLevel(7);
        player.AddHealth(10);

        Assert.AreSame(player, shared.GetEntityWithPlayerName("Ada"));
        Assert.AreSame(player, shared.GetPlayerEntity());
        Assert.AreEqual("Ada", shared.GetPlayer().Name);
        Assert.AreEqual(1, shared.GetPlayer().Level);
        Assert.AreEqual(7, player.GetLevel().Value);
        Assert.AreEqual(10, player.GetHealth().Value);

        player.ReplaceLevel(8);
        player.ReplaceHealth(20);

        Assert.AreEqual(8, player.GetLevel().Value);
        Assert.AreEqual(20, player.GetHealth().Value);
    }

    [Test]
    public void CreatesSharedContextFromAssemblyCSharpAndFeatureAssemblies()
    {
        var schema = AssemblyCSharpContextFactory.CreateSchema();
        var contexts = AssemblyCSharpContextFactory.Create(schema);
        var shared = contexts.GetShared();
        var eventSystems = schema.CreateEventSystems(contexts);
        var cleanupSystems = schema.CreateCleanupSystems(contexts);
        var listener = new ManaListener();

        shared.CreateEntity().AddAnyManaListener(listener);

        var player = shared.SetPlayer("Ada", 1);
        player.AddLevel(7);
        player.AddHealth(10);
        player.AddMana(5);

        var session = shared.SetSession("sample-session");
        var expired = shared.CreateEntity();
        expired.SetExpired(true);
        var destroyed = shared.CreateEntity();
        destroyed.SetDestroyed(true);

        eventSystems.Execute();
        cleanupSystems.Cleanup();

        Assert.AreSame(player, shared.GetEntityWithPlayerName("Ada"));
        Assert.AreSame(session, shared.GetEntityWithSessionId("sample-session"));
        CollectionAssert.Contains(shared.GetEntitiesWithManaValue(5), player);
        Assert.AreEqual(1, listener.Values.Count);
        Assert.AreEqual(5, listener.Values[0]);
        Assert.IsFalse(expired.IsExpired());
        Assert.IsFalse(destroyed.IsEnabled);
    }

    sealed class ManaListener : IAnyManaListener
    {
        public readonly List<int> Values = new List<int>();

        public void OnAnyMana(SharedEntity entity, int value)
        {
            Values.Add(value);
        }
    }
}
