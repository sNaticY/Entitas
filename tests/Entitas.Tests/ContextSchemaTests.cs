using FluentAssertions;
using Xunit;

namespace Entitas.Tests
{
    public class ContextSchemaTests
    {
        [Fact]
        public void BuildsDeterministicSchemaAndAssignsHandleIndexes()
        {
            var handleB = new ComponentHandle<ComponentB>("ComponentB");
            var handleA = new ComponentHandle<ComponentA>("ComponentA");

            var schema = new ContextSchemaBuilder("Main")
                .Add(handleB)
                .Add(handleA)
                .Build();

            schema.Name.Should().Be("Main");
            schema.TotalComponents.Should().Be(2);
            schema.ComponentNames.Should().Equal("ComponentA", "ComponentB");
            schema.ComponentTypes.Should().Equal(typeof(ComponentA), typeof(ComponentB));
            handleA.Index.Should().Be(0);
            handleB.Index.Should().Be(1);
        }

        [Fact]
        public void IgnoresSameHandleRegisteredTwice()
        {
            var handle = new ComponentHandle<ComponentA>("ComponentA");

            var schema = new ContextSchemaBuilder("Main")
                .Add(handle)
                .Add(handle)
                .Build();

            schema.TotalComponents.Should().Be(1);
        }

        [Fact]
        public void ThrowsWhenComponentNameIsRegisteredTwice()
        {
            var first = new ComponentHandle<ComponentA>("Duplicate");
            var second = new ComponentHandle<ComponentB>("Duplicate");
            var builder = new ContextSchemaBuilder("Main").Add(first);

            FluentActions.Invoking(() => builder.Add(second))
                .Should().Throw<EntitasException>();
        }

        [Fact]
        public void ThrowsWhenComponentTypeIsRegisteredTwice()
        {
            var first = new ComponentHandle<ComponentA>("ComponentA");
            var second = new ComponentHandle<ComponentA>("OtherComponentA");
            var builder = new ContextSchemaBuilder("Main").Add(first);

            FluentActions.Invoking(() => builder.Add(second))
                .Should().Throw<EntitasException>();
        }

        [Fact]
        public void CreatesContextInfoForExpectedContext()
        {
            var schema = new ContextSchemaBuilder("Main")
                .Add(new ComponentHandle<ComponentA>("ComponentA"))
                .Build();

            var contextInfo = schema.CreateContextInfo("Main");

            contextInfo.Name.Should().Be("Main");
            contextInfo.ComponentNames.Should().Equal("ComponentA");
            contextInfo.ComponentTypes.Should().Equal(typeof(ComponentA));
        }

        [Fact]
        public void ThrowsWhenContextNameDoesNotMatchSchema()
        {
            var schema = new ContextSchemaBuilder("Main").Build();

            FluentActions.Invoking(() => schema.CreateContextInfo("Config"))
                .Should().Throw<EntitasException>();
        }
    }
}
