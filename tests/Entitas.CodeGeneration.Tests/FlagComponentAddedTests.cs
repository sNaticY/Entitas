using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class FlagComponentAddedTests
    {
        readonly Contexts _contexts;
        readonly MainEntity _entity;
        readonly LoadingAddedListener _listener;
        readonly MyFeatureLoadingEventSystem _system;

        public FlagComponentAddedTests()
        {
            _contexts = new Contexts();
            _entity = _contexts.main.CreateEntity();
            _listener = new LoadingAddedListener(_entity);
            _system = new MyFeatureLoadingEventSystem(_contexts);
        }

        [Fact]
        public void PassesEntityWhenAddedOnSameEntity()
        {
            _entity.SetMyFeatureLoading(true);
            _system.Execute();

            _listener.Entity.Should().BeSameAs(_entity);
        }

        [Fact]
        public void DoesNotPassEntityWhenAddedOnDifferentEntity()
        {
            _contexts.main.CreateEntity().SetMyFeatureLoading(true);
            _system.Execute();

            _listener.Entity.Should().BeNull();
        }
    }

    public class LoadingAddedListener : IMyFeatureLoadingListener
    {
        readonly MainEntity _listener;

        public LoadingAddedListener(MainEntity entity)
        {
            _listener = entity;
            _listener.AddMyFeatureLoadingListener(this);
        }

        public MainEntity? Entity { get; private set; }

        public void OnMyFeatureLoading(MainEntity entity)
        {
            Entity = entity;
        }
    }
}
