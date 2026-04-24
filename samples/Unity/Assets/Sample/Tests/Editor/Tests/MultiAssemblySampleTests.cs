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
        var contexts = MultiAssemblyContextFactory.Create();
        var shared = contexts.GetShared();

        var player = shared.SetPlayer("Ada", 1);
        player.AddHealth(10);

        Assert.AreSame(player, shared.GetEntityWithPlayerName("Ada"));
        Assert.AreSame(player, shared.GetPlayerEntity());
        Assert.AreEqual("Ada", shared.GetPlayer().Name);
        Assert.AreEqual(1, shared.GetPlayer().Level);
        Assert.AreEqual(10, player.GetHealth().Value);

        player.ReplaceHealth(20);

        Assert.AreEqual(20, player.GetHealth().Value);
    }
}
