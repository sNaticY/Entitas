using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class CleanupSystemsTests
    {
        [Fact]
        public void GeneratesCleanupSystems()
        {
            new MainCleanupSystems(new Contexts()).Should().NotBeNull();
        }
    }
}
