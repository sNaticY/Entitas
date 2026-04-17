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
            _contexts = TestContexts.Create();
            _context = _contexts.GetMain();
        }

        [Fact]
        public void RemovesComponent()
        {
            var system = new RemoveMyFeatureUserMainSystem(_contexts);
            var entity = _context.SetUser("Test", 42);

            system.Cleanup();

            entity.HasUser().Should().BeFalse();
            entity.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void DestroysEntity()
        {
            var system = new DestroyMyFeatureLoadingMainSystem(_contexts);
            var entity = _context.CreateEntity();
            entity.SetLoading(true);

            system.Cleanup();

            entity.IsLoading().Should().BeFalse();
            entity.IsEnabled.Should().BeFalse();
        }
    }
}
