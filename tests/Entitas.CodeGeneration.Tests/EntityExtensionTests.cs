using FluentAssertions;
using MyFeature;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class EntityExtensionTests
    {
        readonly MainContext _context;

        public EntityExtensionTests()
        {
            _context = new MainContext();
        }

        [Fact]
        public void SetsAndRemovesFlagComponent()
        {
            var entity = _context.CreateEntity();

            entity.IsLoading().Should().BeFalse();
            entity.SetLoading(true);
            entity.IsLoading().Should().BeTrue();
            entity.SetLoading(false);
            entity.IsLoading().Should().BeFalse();
        }

        [Fact]
        public void UsesSingleFlagComponentInstance()
        {
            var entity1 = _context.CreateEntity();
            var entity2 = _context.CreateEntity();

            entity1.SetLoading(true);
            entity2.SetLoading(true);

            entity1.GetComponent(MainComponentsLookup.MyFeatureLoading)
                .Should().BeSameAs(entity2.GetComponent(MainComponentsLookup.MyFeatureLoading));
        }

        [Fact]
        public void AddsGetsAndReplacesComponent()
        {
            var entity = _context.CreateEntity();
            entity.AddUser("Test", 42);

            entity.HasUser().Should().BeTrue();
            entity.GetUser().Name.Should().Be("Test");
            entity.GetUser().Age.Should().Be(42);

            entity.ReplaceUser("Replaced", 24);
            entity.GetUser().Name.Should().Be("Replaced");
            entity.GetUser().Age.Should().Be(24);
        }

        [Fact]
        public void AddComponentUsesComponentPool()
        {
            var component = new UserComponent { Name = "Pooled", Age = 24 };
            var entity = _context.CreateEntity();
            entity.GetComponentPool(MainComponentsLookup.MyFeatureUser).Push(component);

            entity.AddUser("Test", 42);

            entity.GetUser().Should().BeSameAs(component);
        }

        [Fact]
        public void ReplaceComponentUsesComponentPool()
        {
            var component = new UserComponent { Name = "Pooled", Age = 24 };
            var entity = _context.CreateEntity();
            entity.GetComponentPool(MainComponentsLookup.MyFeatureUser).Push(component);

            entity.ReplaceUser("Test", 42);

            entity.GetUser().Should().BeSameAs(component);
        }

        [Fact]
        public void RemovesComponent()
        {
            var entity = _context.CreateEntity();
            entity.AddUser("Test", 42);

            entity.RemoveUser();

            entity.HasUser().Should().BeFalse();
        }
    }
}
