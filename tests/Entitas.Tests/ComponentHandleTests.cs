using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Entitas.Tests
{
    public class ComponentHandleTests
    {
        [Fact]
        public void ExposesComponentMetadata()
        {
            var handle = new ComponentHandle<ComponentA>("ComponentA");

            handle.Name.Should().Be("ComponentA");
            handle.ComponentType.Should().Be(typeof(ComponentA));
            handle.IsAssigned.Should().BeFalse();
        }

        [Fact]
        public void ThrowsWhenReadingUnassignedIndex()
        {
            var handle = new ComponentHandle<ComponentA>("ComponentA");

            FluentActions.Invoking(() => _ = handle.Index)
                .Should().Throw<EntitasException>()
                .WithMessage("*ComponentA*ContextSchemaBuilder*");
        }

        [Fact]
        public void AssignsIndexOnce()
        {
            var handle = new ComponentHandle<ComponentA>("ComponentA");

            handle.AssignIndex(CID.ComponentA);
            handle.AssignIndex(CID.ComponentA);

            handle.Index.Should().Be(CID.ComponentA);
            handle.IsAssigned.Should().BeTrue();
        }

        [Fact]
        public void ThrowsWhenReassignedToDifferentIndex()
        {
            var handle = new ComponentHandle<ComponentA>("ComponentA");
            handle.AssignIndex(CID.ComponentA);

            FluentActions.Invoking(() => handle.AssignIndex(CID.ComponentB))
                .Should().Throw<EntitasException>();
        }

        [Fact]
        public void EntityComponentApisAcceptHandles()
        {
            var handle = new ComponentHandle<ComponentA>("ComponentA");
            handle.AssignIndex(CID.ComponentA);
            var entity = new TestEntity();
            entity.Initialize(0, CID.TotalComponents, new Stack<IComponent>[CID.TotalComponents]);

            entity.AddComponent(handle, Component.A);

            entity.HasComponent(handle).Should().BeTrue();
            entity.GetComponent(handle).Should().BeSameAs(Component.A);

            entity.ReplaceComponent(handle, Component.B);
            entity.GetComponent(handle).Should().BeSameAs(Component.B);

            entity.RemoveComponent(handle);
            entity.HasComponent(handle).Should().BeFalse();
        }

        [Fact]
        public void EntityPoolingApisAcceptHandles()
        {
            var handle = new ComponentHandle<ComponentA>("ComponentA");
            handle.AssignIndex(CID.ComponentA);
            var entity = new TestEntity();
            entity.Initialize(0, CID.TotalComponents, new Stack<IComponent>[CID.TotalComponents]);
            entity.GetComponentPool(handle).Push(Component.A);

            entity.CreateComponent(handle, typeof(ComponentA)).Should().BeSameAs(Component.A);
        }
    }
}
