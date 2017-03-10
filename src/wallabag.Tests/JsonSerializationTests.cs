using System;
using System.Collections.ObjectModel;
using wallabag.Data.Common;
using wallabag.Data.Models;
using Xunit;

namespace wallabag.Tests
{
    public class JsonSerializationTests
    {
        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        [InlineData(typeof(bool))]
        [InlineData(typeof(Tag))]
        [InlineData(typeof(Item))]
        [InlineData(typeof(Language))]
        public void SubmittingANotSupportedTypeReturnsFalse(Type t)
        {
            var converter = new JsonTagConverter();
            Assert.False(converter.CanConvert(t));
        }

        [Fact]
        public void SubmittingASupportedTypeReturnsTrue()
        {
            var converter = new JsonTagConverter();
            Assert.True(converter.CanConvert(typeof(ObservableCollection<Tag>)));
        }

        [Fact]
        public void ConverterCanWrite()
        {
            var converter = new JsonTagConverter();
            Assert.True(converter.CanWrite);
        }

        [Fact]
        public void ConverterCanRead()
        {
            var converter = new JsonTagConverter();
            Assert.True(converter.CanRead);
        }
    }
}
