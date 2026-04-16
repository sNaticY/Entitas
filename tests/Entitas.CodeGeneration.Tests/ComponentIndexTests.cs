using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests
{
    public class ComponentIndexTests
    {
        [Fact]
        public void GeneratesLookupConstants()
        {
            MainComponentsLookup.MyFeatureLoading.Should().BeGreaterThanOrEqualTo(0);
            MainComponentsLookup.MyFeatureUser.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public void IncludesComponentNames()
        {
            MainComponentsLookup.componentNames.Should().Contain("MyFeatureLoading");
            MainComponentsLookup.componentNames.Should().Contain("MyFeatureUser");
        }
    }
}
