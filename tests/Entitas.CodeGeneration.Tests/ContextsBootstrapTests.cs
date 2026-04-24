using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests;

public class ContextsBootstrapTests
{
    [Fact]
    public void RegistersContextsViaGeneratedRegistrationExtensions()
    {
        var contexts = new Contexts()
            .RegisterMain()
            .RegisterConfig();

        contexts.GetMain().Should().NotBeNull();
        contexts.GetConfig().Should().NotBeNull();
        contexts.GetMain().GetEntityIndex(MainEntityIndices.MyFeatureUserName).Should().NotBeNull();
        contexts.GetConfig().GetEntityIndex(ConfigEntityIndices.MyFeatureSettingsKey).Should().NotBeNull();
    }

    [Fact]
    public void GetsMultipleRegisteredContextsViaGeneratedAccessors()
    {
        var contexts = TestContexts.Create();

        contexts.GetMain().Should().NotBeNull();
        contexts.GetConfig().Should().NotBeNull();
    }

    [Fact]
    public void InitializesEntityIndicesForAllRegisteredContexts()
    {
        var contexts = TestContexts.Create();

        contexts.GetMain().GetEntityIndex(MainEntityIndices.MyFeatureUserName).Should().NotBeNull();
        contexts.GetConfig().GetEntityIndex(ConfigEntityIndices.MyFeatureSettingsKey).Should().NotBeNull();
        contexts.GetConfig().GetEntityIndex(ConfigEntityIndices.MyFeatureSettingsVersion).Should().NotBeNull();
    }

    [Fact]
    public void UsesGeneratedEntityIndexApisAcrossMultipleContexts()
    {
        var contexts = TestContexts.Create();
        var config = contexts.GetConfig();
        var entity = config.SetSettings("render", 3);

        config.GetEntityWithMyFeatureSettingsKey("render").Should().BeSameAs(entity);
        config.GetEntitiesWithMyFeatureSettingsVersion(3).Should().Contain(entity);
    }
}
