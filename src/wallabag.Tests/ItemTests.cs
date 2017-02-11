using System;
using wallabag.Data.Models;
using Xunit;

namespace wallabag.Tests
{
    public class ItemTests
    {
        [Fact]
        public void ToStringReturnsTitle()
        {
            string title = "This is a test";

            var item = new Item() { Title = title };
            Assert.Equal(title, item.ToString());
        }

        [Fact]
        public void EqualityCheckRespectsIdAndModificationDate()
        {
            var dateTime = DateTime.Now;

            var item1 = new Item()
            {
                Id = 1,
                LastModificationDate = dateTime
            };
            var item2 = new Item()
            {
                Id = 2,
                LastModificationDate = dateTime
            };

            Assert.NotEqual(item1, item2);

            item2.Id = 1;

            Assert.Equal(item1, item2);

            item2.LastModificationDate = new DateTime(2017, 1, 1);

            Assert.NotEqual(item1, item2);
        }

        [Fact]
        public void ComparisonFailsWithWrongType()
        {
            Assert.Throws<NullReferenceException>(() => new Item() { CreationDate = DateTime.Now }.CompareTo(new Tag()));
        }

        [Fact]
        public void ComparisonWithDifferentCreationDatesDoesNotReturnNull()
        {
            var dateTime = DateTime.Now;

            var item1 = new Item() { CreationDate = dateTime };
            var item2 = new Item() { CreationDate = dateTime.AddDays(1) };

            Assert.NotEqual(0, item1.CompareTo(item2));
        }

        [Fact]
        public void ComparisonWithSameCreationDatesAndDifferentIdReturnsIDDifference()
        {
            var dateTime = DateTime.Now;

            var item1 = new Item() { Id = 1, CreationDate = dateTime };
            var item2 = new Item() { Id = 10, CreationDate = dateTime };

            Assert.Equal(9, item1.CompareTo(item2));
        }
    }
}
