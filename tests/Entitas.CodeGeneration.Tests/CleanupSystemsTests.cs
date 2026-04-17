using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class CleanupSystemsTests
    {
        [Fact]
        public void GeneratesCleanupSystems()
        {
            new MainCleanupSystems(TestContexts.Create()).Should().NotBeNull();
        }
    }
}
