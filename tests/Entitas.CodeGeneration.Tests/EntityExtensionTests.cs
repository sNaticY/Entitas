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

            entity.IsMyFeatureLoading().Should().BeFalse();
            entity.SetMyFeatureLoading(true);
            entity.IsMyFeatureLoading().Should().BeTrue();
            entity.SetMyFeatureLoading(false);
            entity.IsMyFeatureLoading().Should().BeFalse();
        }

        [Fact]
        public void UsesSingleFlagComponentInstance()
        {
            var entity1 = _context.CreateEntity();
            var entity2 = _context.CreateEntity();

            entity1.SetMyFeatureLoading(true);
            entity2.SetMyFeatureLoading(true);

            entity1.GetComponent(MainComponentsLookup.MyFeatureLoading)
                .Should().BeSameAs(entity2.GetComponent(MainComponentsLookup.MyFeatureLoading));
        }

        [Fact]
        public void AddsGetsAndReplacesComponent()
        {
            var entity = _context.CreateEntity();
            entity.AddMyFeatureUser("Test", 42);

            entity.HasMyFeatureUser().Should().BeTrue();
            entity.GetMyFeatureUser().Name.Should().Be("Test");
            entity.GetMyFeatureUser().Age.Should().Be(42);

            entity.ReplaceMyFeatureUser("Replaced", 24);
            entity.GetMyFeatureUser().Name.Should().Be("Replaced");
            entity.GetMyFeatureUser().Age.Should().Be(24);
        }

        [Fact]
        public void AddComponentUsesComponentPool()
        {
            var component = new UserComponent { Name = "Pooled", Age = 24 };
            var entity = _context.CreateEntity();
            entity.GetComponentPool(MainComponentsLookup.MyFeatureUser).Push(component);

            entity.AddMyFeatureUser("Test", 42);

            entity.GetMyFeatureUser().Should().BeSameAs(component);
        }

        [Fact]
        public void ReplaceComponentUsesComponentPool()
        {
            var component = new UserComponent { Name = "Pooled", Age = 24 };
            var entity = _context.CreateEntity();
            entity.GetComponentPool(MainComponentsLookup.MyFeatureUser).Push(component);

            entity.ReplaceMyFeatureUser("Test", 42);

            entity.GetMyFeatureUser().Should().BeSameAs(component);
        }

        [Fact]
        public void RemovesComponent()
        {
            var entity = _context.CreateEntity();
            entity.AddMyFeatureUser("Test", 42);

            entity.RemoveMyFeatureUser();

            entity.HasMyFeatureUser().Should().BeFalse();
        }
    }
}
