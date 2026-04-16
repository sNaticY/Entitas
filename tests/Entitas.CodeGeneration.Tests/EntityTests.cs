using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class EntityTests
    {
        [Fact]
        public void GeneratesEntity()
        {
            var entity = new MainEntity();
            entity.Should().NotBeNull();
            entity.Should().BeAssignableTo<Entity>();
        }
    }
}
