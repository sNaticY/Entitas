using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class AnyFlagComponentRemovedTests
    {
        readonly Contexts _contexts;
        readonly MainContext _context;
        readonly AnyLoadingRemovedListener _listener;
        readonly MyFeatureAnyLoadingRemovedEventSystem _system;

        public AnyFlagComponentRemovedTests()
        {
            _contexts = TestContexts.Create();
            _context = _contexts.GetMain();
            _listener = new AnyLoadingRemovedListener(_context);
            _system = new MyFeatureAnyLoadingRemovedEventSystem(_contexts);
        }

        [Fact]
        public void PassesEntityWhenRemoved()
        {
            var entity = _context.CreateEntity();
            entity.SetLoading(true);
            _system.Execute();
            _listener.Entity.Should().BeNull();

            entity.SetLoading(false);
            _system.Execute();

            _listener.Entity.Should().BeSameAs(entity);
        }
    }

    public class AnyLoadingRemovedListener : IMyFeatureAnyLoadingRemovedListener
    {
        readonly MainEntity _listener;

        public AnyLoadingRemovedListener(MainContext context)
        {
            _listener = context.CreateEntity();
            _listener.AddMyFeatureAnyLoadingRemovedListener(this);
        }

        public MainEntity? Entity { get; private set; }

        public void OnMyFeatureAnyLoadingRemoved(MainEntity entity)
        {
            Entity = entity;
        }
    }
}
