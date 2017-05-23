using Xunit;
using wallabag.Data.Models;
using FakeItEasy;
using wallabag.Data.Interfaces;

namespace wallabag.Tests
{
    public class SearchPropertiesTests
    {
        [Fact]
        public void ChangingTheQueryFromEmptyToSomethingFiresSearchStartedEvent()
        {
            var sp = new SearchProperties() { Query = string.Empty };
            string newQuery = "Test Query";
            bool searchStarted = false;

            sp.SearchStarted += (s, e) =>
            {
                searchStarted = true;
                Assert.Equal(newQuery, e.Query);
            };

            sp.Query = newQuery;
            Assert.True(searchStarted);
        }

        [Fact]
        public void ChangingTheQueryFromSomethingToEmptyFiresSearchCanceledEvent()
        {
            var sp = new SearchProperties();
            bool searchCanceled = false;

            sp.Query = "test";

            sp.SearchCanceled += (s, e) =>
            {
                searchCanceled = true;
                Assert.Equal(string.Empty, e.Query);
            };

            sp.Query = string.Empty;
            Assert.True(searchCanceled);
        }

        [Fact]
        public void ReplacingOneSearchPropertyWithAnotherActuallyReplacesAllProperties()
        {
            var fakePlatform = A.Fake<IPlatformSpecific>();
            var firstProperty = new SearchProperties()
            {
                Query = "My first query",
                ItemTypeIndex = 0,
                Language = Language.GetUnknown(fakePlatform),
                OrderAscending = true,
                SortType = SearchProperties.SearchPropertiesSortType.ByCreationDate,
                Tag = default(Tag)
            };
            var secondProperty = new SearchProperties()
            {
                Query = "My second query",
                ItemTypeIndex = 1,
                Language = new Language("de"),
                OrderAscending = false,
                SortType = SearchProperties.SearchPropertiesSortType.ByReadingTime,
                Tag = new Tag() { Label = "test" }
            };

            firstProperty.Replace(secondProperty);

            Assert.Equal("My second query", firstProperty.Query);
            Assert.Equal(1, firstProperty.ItemTypeIndex);
            Assert.Equal(new Language("de"), firstProperty.Language);
            Assert.Equal(false, firstProperty.OrderAscending);
            Assert.Equal(SearchProperties.SearchPropertiesSortType.ByReadingTime, firstProperty.SortType);
            Assert.Equal("test", firstProperty.Tag.Label);
        }
    }
}
