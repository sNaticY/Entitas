using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class EntityIndexExtensionTests
    {
        readonly MainContext _context;

        public EntityIndexExtensionTests()
        {
            _context = TestContexts.Create().GetMain();
        }

        [Fact]
        public void AddsAllEntityIndexes()
        {
            _context.GetEntityIndex(MainEntityIndices.MyFeatureUserName).Should().BeAssignableTo<PrimaryEntityIndex<MainEntity, string>>();
            _context.GetEntityIndex(MainEntityIndices.MyFeatureUserAge).Should().BeAssignableTo<EntityIndex<MainEntity, int>>();
        }

        [Fact]
        public void GetsEntity()
        {
            var user = _context.CreateEntity();
            user.AddMyFeatureUser("Test", 42);

            var entity = _context.GetEntityWithMyFeatureUserName("Test");
            entity.Should().BeSameAs(user);
        }

        [Fact]
        public void GetsEntities()
        {
            var user1 = _context.CreateEntity();
            user1.AddMyFeatureUser("Test1", 42);
            var user2 = _context.CreateEntity();
            user2.AddMyFeatureUser("Test2", 42);

            var entities = _context.GetEntitiesWithMyFeatureUserAge(42);
            entities.Should().HaveCount(2);
            entities.Should().Contain(user1);
            entities.Should().Contain(user2);
        }
    }
}
