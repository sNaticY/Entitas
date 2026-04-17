using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class AnyFlagComponentAddedTests
    {
        readonly Contexts _contexts;
        readonly MainContext _context;
        readonly AnyLoadingAddedListener _listener;
        readonly MyFeatureAnyLoadingEventSystem _system;

        public AnyFlagComponentAddedTests()
        {
            _contexts = TestContexts.Create();
            _context = _contexts.GetMain();
            _listener = new AnyLoadingAddedListener(_context);
            _system = new MyFeatureAnyLoadingEventSystem(_contexts);
        }

        [Fact]
        public void IsNullWhenNothingChanged()
        {
            _system.Execute();
            _listener.Entity.Should().BeNull();
        }

        [Fact]
        public void PassesEntityWhenAdded()
        {
            var entity = _context.CreateEntity();
            entity.SetLoading(true);

            _system.Execute();

            _listener.Entity.Should().BeSameAs(entity);
        }
    }

    public class AnyLoadingAddedListener : IMyFeatureAnyLoadingListener
    {
        readonly MainEntity _listener;

        public AnyLoadingAddedListener(MainContext context)
        {
            _listener = context.CreateEntity();
            _listener.AddMyFeatureAnyLoadingListener(this);
        }

        public MainEntity? Entity { get; private set; }

        public void OnMyFeatureAnyLoading(MainEntity entity)
        {
            Entity = entity;
        }
    }
}
