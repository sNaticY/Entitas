using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class EventSystemsTests
    {
        [Fact]
        public void GeneratesEventSystems()
        {
            new MainEventSystems(new Contexts()).Should().NotBeNull();
        }
    }
}
