using System.Collections.Generic;
using Xunit;

namespace wallabag.Tests
{
    public class ListHelperTests
    {
        [Fact]
        public void AddingAnItemInAscendingOrderPutsItInRow()
        {
            var items = CreateTestList();

            string testItem = "Item 3.5";
            Data.Common.Helpers.ListHelper.AddSorted(items, testItem, sortAscending: true);

            Assert.True(items.Count == 10);
            Assert.True(items.Contains(testItem));
            Assert.True(items[4] == testItem);
        }

        [Fact]
        public void AddingAnItemByDefaultUsesTheDescendingOrder()
        {
            var items = CreateTestList();

            string testItem = "Item 3.5";
            Data.Common.Helpers.ListHelper.AddSorted(items, testItem, sortAscending: false);

            Assert.True(items.Count == 10);
            Assert.True(items.Contains(testItem));
            Assert.True(items[0] == testItem);
        }

        [Fact]
        public void ReplacingACollectionReplacesAllItems()
        {
            var items = CreateTestList();
            var newItems = new List<string>();

            for (int i = 0; i < 3; i++)
                newItems.Add($"New {i}");

            Data.Common.Helpers.ListHelper.Replace(items, newItems);

            Assert.True(items.Count == newItems.Count);
            Assert.All(items, item =>
            {
                Assert.Matches("New [0-9]", item);
            });
        }

        private List<string> CreateTestList()
        {
            var items = new List<string>();

            for (int i = 0; i < 9; i++)
                items.Add($"Item {i}");

            return items;
        }
    }
}
