using System;
using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class ContextExtensionTests
    {
        readonly MainContext _context;

        public ContextExtensionTests()
        {
            _context = new MainContext();
        }

        [Fact]
        public void SetsAndUnsetsFlagComponent()
        {
            _context.IsLoading().Should().BeFalse();

            _context.SetLoading(true);
            _context.IsLoading().Should().BeTrue();
            _context.GetLoadingEntity().Should().NotBeNull();

            _context.SetLoading(false);
            _context.IsLoading().Should().BeFalse();
        }

        [Fact]
        public void ReusesSameFlagEntity()
        {
            _context.SetLoading(true);
            var first = _context.GetLoadingEntity();

            _context.SetLoading(true);
            _context.GetLoadingEntity().Should().BeSameAs(first);
        }

        [Fact]
        public void SetsAndGetsUniqueComponent()
        {
            var entity = _context.SetUser("Test", 42);

            _context.HasUser().Should().BeTrue();
            _context.GetUserEntity().Should().BeSameAs(entity);
            _context.GetUser().Name.Should().Be("Test");
            _context.GetUser().Age.Should().Be(42);
        }

        [Fact]
        public void ThrowsWhenSettingUniqueComponentTwice()
        {
            _context.SetUser("Test", 42);

            FluentActions.Invoking(() => _context.SetUser("Again", 24))
                .Should().Throw<EntitasException>();
        }

        [Fact]
        public void ReplacesUniqueComponent()
        {
            _context.SetUser("Test", 42);
            _context.ReplaceUser("Replaced", 24);

            _context.GetUser().Name.Should().Be("Replaced");
            _context.GetUser().Age.Should().Be(24);
        }

        [Fact]
        public void RemovesAndDestroysUniqueEntity()
        {
            var entity = _context.SetUser("Test", 42);

            _context.RemoveUser();

            _context.HasUser().Should().BeFalse();
            entity.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public void ThrowsWhenGettingUnsetUniqueComponent()
        {
            FluentActions.Invoking(() => _context.GetUser())
                .Should().Throw<NullReferenceException>();
        }
    }
}
