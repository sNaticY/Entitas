using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class CleanupSystemTests
    {
        readonly Contexts _contexts;
        readonly MainContext _context;

        public CleanupSystemTests()
        {
            _contexts = new Contexts();
            _context = _contexts.main;
        }

        [Fact]
        public void RemovesComponent()
        {
            var system = new RemoveMyFeatureUserMainSystem(_contexts);
            var entity = _context.SetMyFeatureUser("Test", 42);

            system.Cleanup();

            entity.HasMyFeatureUser().Should().BeFalse();
            entity.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void DestroysEntity()
        {
            var system = new DestroyMyFeatureLoadingMainSystem(_contexts);
            var entity = _context.CreateEntity();
            entity.SetMyFeatureLoading(true);

            system.Cleanup();

            entity.IsMyFeatureLoading().Should().BeFalse();
            entity.IsEnabled.Should().BeFalse();
        }
    }
}
