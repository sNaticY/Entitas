using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class ContextTests
    {
        [Fact]
        public void GeneratesContext()
        {
            var context = new MainContext();
            context.Should().NotBeNull();
            context.Should().BeAssignableTo<Context<MainEntity>>();
        }

        [Fact]
        public void CreatesEntity()
        {
            var context = new MainContext();
            var entity = context.CreateEntity();
            entity.Should().NotBeNull();
            entity.Should().BeAssignableTo<MainEntity>();
            entity.TotalComponents.Should().Be(context.TotalComponents);
        }

        [Fact]
        public void CreatesContextFromGeneratedSchemaBuilder()
        {
            var schema = MainContext.CreateSchemaBuilder()
                .AddMainUser()
                .Build();

            var context = new MainContext(schema);

            context.ContextInfo.Name.Should().Be("Main");
            context.TotalComponents.Should().Be(1);
        }
    }
}
