using System;
using FluentAssertions;
using Xunit;

namespace Entitas.Tests
{
    public class ContextsTests
    {
        [Fact]
        public void RegistersAndGetsContextByType()
        {
            var main = new TestContext(CID.TotalComponents);
            var contexts = new Contexts().Register(main);

            contexts.Get<TestContext>().Should().BeSameAs(main);
        }

        [Fact]
        public void TriesGetRegisteredContext()
        {
            var main = new TestContext(CID.TotalComponents);
            var contexts = new Contexts().Register(main);

            var found = contexts.TryGet<TestContext>(out var resolved);

            found.Should().BeTrue();
            resolved.Should().BeSameAs(main);
        }

        [Fact]
        public void ReturnsFalseWhenContextIsNotRegistered()
        {
            var contexts = new Contexts();

            var found = contexts.TryGet<TestContext>(out var resolved);

            found.Should().BeFalse();
            resolved.Should().BeNull();
        }

        [Fact]
        public void ThrowsWhenGettingMissingContext()
        {
            var contexts = new Contexts();

            FluentActions.Invoking(() => contexts.Get<TestContext>())
                .Should().Throw<InvalidOperationException>()
                .WithMessage("*TestContext*");
        }

        [Fact]
        public void ThrowsWhenRegisteringSameContextTypeTwice()
        {
            var contexts = new Contexts().Register(new TestContext(CID.TotalComponents));

            FluentActions.Invoking(() => contexts.Register(new TestContext(CID.TotalComponents)))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("*TestContext*");
        }

        [Fact]
        public void ReturnsAllRegisteredContexts()
        {
            var main = new TestContext(CID.TotalComponents);
            var other = new OtherTestContext();
            var contexts = new Contexts()
                .Register(main)
                .Register(other);

            contexts.AllContexts.Should().BeEquivalentTo(new IContext[] { main, other });
        }
    }
}
