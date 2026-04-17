using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class ListenerEventEntityExtensionTests
    {
        readonly MainContext _context;
        readonly ListenerEventEntityExtensionListener _listener;

        public ListenerEventEntityExtensionTests()
        {
            _context = TestContexts.Create().GetMain();
            _listener = new ListenerEventEntityExtensionListener();
        }

        [Fact]
        public void AddsListener()
        {
            var listener = _context.CreateEntity();
            listener.AddMyFeatureAnyLoadingListener(_listener);

            listener.HasMyFeatureAnyLoadingListener().Should().BeTrue();
        }

        [Fact]
        public void RemovesListener()
        {
            var listener = _context.CreateEntity();
            listener.AddMyFeatureAnyLoadingListener(_listener);
            listener.RemoveMyFeatureAnyLoadingListener(_listener, false);

            listener.GetMyFeatureAnyLoadingListener().value.Should().HaveCount(0);
        }
    }

    public class ListenerEventEntityExtensionListener : IMyFeatureAnyLoadingListener
    {
        public void OnMyFeatureAnyLoading(MainEntity entity) { }
    }
}
