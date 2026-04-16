using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class EntityIndexTests
    {
        [Fact]
        public void InitializesEntityIndexesViaContextsPostConstructor()
        {
            var contexts = new Contexts();

            contexts.main.GetEntityIndex(Contexts.MyFeatureUserName).Should().NotBeNull();
            contexts.main.GetEntityIndex(Contexts.MyFeatureUserAge).Should().NotBeNull();
        }
    }
}
