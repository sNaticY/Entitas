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
            _context.IsMyFeatureLoading().Should().BeFalse();

            _context.SetMyFeatureLoading(true);
            _context.IsMyFeatureLoading().Should().BeTrue();
            _context.GetMyFeatureLoadingEntity().Should().NotBeNull();

            _context.SetMyFeatureLoading(false);
            _context.IsMyFeatureLoading().Should().BeFalse();
        }

        [Fact]
        public void ReusesSameFlagEntity()
        {
            _context.SetMyFeatureLoading(true);
            var first = _context.GetMyFeatureLoadingEntity();

            _context.SetMyFeatureLoading(true);
            _context.GetMyFeatureLoadingEntity().Should().BeSameAs(first);
        }

        [Fact]
        public void SetsAndGetsUniqueComponent()
        {
            var entity = _context.SetMyFeatureUser("Test", 42);

            _context.HasMyFeatureUser().Should().BeTrue();
            _context.GetMyFeatureUserEntity().Should().BeSameAs(entity);
            _context.GetMyFeatureUser().Name.Should().Be("Test");
            _context.GetMyFeatureUser().Age.Should().Be(42);
        }

        [Fact]
        public void ThrowsWhenSettingUniqueComponentTwice()
        {
            _context.SetMyFeatureUser("Test", 42);

            FluentActions.Invoking(() => _context.SetMyFeatureUser("Again", 24))
                .Should().Throw<EntitasException>();
        }

        [Fact]
        public void ReplacesUniqueComponent()
        {
            _context.SetMyFeatureUser("Test", 42);
            _context.ReplaceMyFeatureUser("Replaced", 24);

            _context.GetMyFeatureUser().Name.Should().Be("Replaced");
            _context.GetMyFeatureUser().Age.Should().Be(24);
        }

        [Fact]
        public void RemovesAndDestroysUniqueEntity()
        {
            var entity = _context.SetMyFeatureUser("Test", 42);

            _context.RemoveMyFeatureUser();

            _context.HasMyFeatureUser().Should().BeFalse();
            entity.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public void ThrowsWhenGettingUnsetUniqueComponent()
        {
            FluentActions.Invoking(() => _context.GetMyFeatureUser())
                .Should().Throw<NullReferenceException>();
        }
    }
}
