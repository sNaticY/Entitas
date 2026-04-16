using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class FlagComponentRemovedTests
    {
        readonly Contexts _contexts;
        readonly MainEntity _entity;
        readonly LoadingRemovedListener _listener;
        readonly MyFeatureLoadingRemovedEventSystem _system;

        public FlagComponentRemovedTests()
        {
            _contexts = new Contexts();
            _entity = _contexts.main.CreateEntity();
            _listener = new LoadingRemovedListener(_entity);
            _system = new MyFeatureLoadingRemovedEventSystem(_contexts);
        }

        [Fact]
        public void PassesEntityWhenRemovedOnSameEntity()
        {
            _entity.SetMyFeatureLoading(true);
            _system.Execute();
            _listener.Entity.Should().BeNull();

            _entity.SetMyFeatureLoading(false);
            _system.Execute();

            _listener.Entity.Should().BeSameAs(_entity);
        }
    }

    public class LoadingRemovedListener : IMyFeatureLoadingRemovedListener
    {
        readonly MainEntity _listener;

        public LoadingRemovedListener(MainEntity entity)
        {
            _listener = entity;
            _listener.AddMyFeatureLoadingRemovedListener(this);
        }

        public MainEntity? Entity { get; private set; }

        public void OnMyFeatureLoadingRemoved(MainEntity entity)
        {
            Entity = entity;
        }
    }
}
