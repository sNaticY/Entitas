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

        [Fact]
        public void CreatesCleanupSystemsFromRegisteredFactories()
        {
            var cleanupA = new CleanupSpy();
            var cleanupB = new CleanupSpy();
            var contexts = new Contexts();
            var schema = new ContextSchemaBuilder("Main")
                .AddCleanupSystem("B", _ => cleanupB)
                .AddCleanupSystem("A", _ => cleanupA)
                .Build();

            var systems = schema.CreateCleanupSystems(contexts);
            systems.Cleanup();

            cleanupA.Calls.Should().Be(1);
            cleanupB.Calls.Should().Be(1);
        }

        [Fact]
        public void CreatesEventSystemsByPriorityThenName()
        {
            var calls = new System.Collections.Generic.List<string>();
            var contexts = new Contexts();
            var schema = new ContextSchemaBuilder("Main")
                .AddEventSystem("Late", 10, _ => new ExecuteSpy("Late", calls))
                .AddEventSystem("EarlyB", 0, _ => new ExecuteSpy("EarlyB", calls))
                .AddEventSystem("EarlyA", 0, _ => new ExecuteSpy("EarlyA", calls))
                .Build();

            var systems = schema.CreateEventSystems(contexts);
            systems.Execute();

            calls.Should().Equal("EarlyA", "EarlyB", "Late");
        }

        [Fact]
        public void ThrowsWhenSystemRegistrationNameIsRegisteredTwice()
        {
            var builder = new ContextSchemaBuilder("Main")
                .AddCleanupSystem("Cleanup", _ => new CleanupSpy());

            FluentActions.Invoking(() => builder.AddCleanupSystem("Cleanup", _ => new CleanupSpy()))
                .Should().Throw<EntitasException>();
        }

        sealed class CleanupSpy : ICleanupSystem
        {
            public int Calls { get; private set; }

            public void Cleanup()
            {
                Calls++;
            }
        }

        sealed class ExecuteSpy : IExecuteSystem
        {
            readonly string _name;
            readonly System.Collections.Generic.List<string> _calls;

            public ExecuteSpy(string name, System.Collections.Generic.List<string> calls)
            {
                _name = name;
                _calls = calls;
            }

            public void Execute()
            {
                _calls.Add(_name);
            }
        }
    }
}
