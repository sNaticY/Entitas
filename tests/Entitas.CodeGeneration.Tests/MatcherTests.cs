using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class MatcherTests
    {
        [Fact]
        public void GeneratesAllOfMatcher()
        {
            var matcher = MainMatcher.AllOf(1);
            matcher.Should().BeAssignableTo<IAllOfMatcher<MainEntity>>();
            matcher.Should().BeEquivalentTo(Entitas.Matcher<MainEntity>.AllOf(1));
        }

        [Fact]
        public void GeneratesAnyOfMatcher()
        {
            var matcher = MainMatcher.AnyOf(1);
            matcher.Should().BeAssignableTo<IAnyOfMatcher<MainEntity>>();
            matcher.Should().BeEquivalentTo(Entitas.Matcher<MainEntity>.AnyOf(1));
        }

        [Fact]
        public void GeneratesNamedMatcher()
        {
            var matcher = MainMatcher.MyFeatureUser();
            matcher.Should().BeAssignableTo<IMatcher<MainEntity>>();
        }
    }
}
