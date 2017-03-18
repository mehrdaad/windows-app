using System;
using wallabag.Data.Common.Helpers;
using Xunit;

namespace wallabag.Tests
{
    public class UriHelperTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("/test")]
        [InlineData("/test/")]
        [InlineData("/test/123")]
        [InlineData("/test//123")]
        [InlineData("/test/t123/")]
        public void AppendingOfSubstringReturnsValidUri(string substring)
        {
            var uriWithoutPortNumber = new Uri("http://localhost").Append(substring);
            Assert.EndsWith(substring, uriWithoutPortNumber.ToString());

            var uriWithPortNumber = new Uri("http://localhost:8000").Append(substring);
            Assert.EndsWith(substring, uriWithPortNumber.ToString());
        }

        [Fact]
        public void AppendingWithoutAbsoluteBaseUriThrowsException()
        {
            Assert.Throws<FormatException>(() => new Uri("/123", UriKind.Relative).Append(""));
        }

        [Theory]
        [InlineData("http://localhost", true)]
        [InlineData("https://localhost", true)]
        [InlineData("https://localhost/", true)]
        [InlineData("test://localhost/", true)]
        [InlineData("http://localhost:123/", true)]
        [InlineData("http://localhost:test/", false)]
        [InlineData("localhost:test", true)]
        [InlineData("http:///", false)]
        [InlineData("localhost", true)]
        public void UriStringReturnsExpectedResult(string uriString, bool expectedResult)
        {
            Assert.Equal(expectedResult, uriString.IsValidUri());
        }
    }
}
