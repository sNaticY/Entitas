using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class EntityIndexTests
    {
        [Fact]
        public void InitializesEntityIndexesViaExplicitBootstrap()
        {
            var contexts = TestContexts.Create();

            contexts.GetMain().GetEntityIndex(MainEntityIndices.MyFeatureUserName).Should().NotBeNull();
            contexts.GetMain().GetEntityIndex(MainEntityIndices.MyFeatureUserAge).Should().NotBeNull();
        }
    }
}
