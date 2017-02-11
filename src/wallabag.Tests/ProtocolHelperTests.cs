using System;
using wallabag.Data.Common.Helpers;
using Xunit;

namespace wallabag.Tests
{
    public class ProtocolHelperTests
    {
        [Fact]
        public void ValidProtocolUriReturnsCorrectValues()
        {
            string protocol = "wallabag://username@https://localhost/";
            var result = ProtocolHelper.Parse(protocol);

            Assert.NotNull(result);
            Assert.Equal(result.Server, "https://localhost/");
            Assert.Equal(result.Username, "username");
        }

        [Fact]
        public void InvalidProtocolHandlerThrowsArgumentException()
        {
            string protocol = "mytest://username@https://localhost/";
            Assert.Throws<ArgumentException>(() => ProtocolHelper.Parse(protocol));
        }

        [Fact]
        public void EmptyUsernameReturnsValidResult()
        {
            string protocol = "wallabag://@https://localhost/";
            var result = ProtocolHelper.Parse(protocol);

            Assert.NotNull(result);
            Assert.Equal(result.Server, "https://localhost/");
            Assert.Equal(result.Username, string.Empty);
        }

        [Fact]
        public void EmptyServerReturnsValidResult()
        {
            string protocol = "wallabag://username@";
            var result = ProtocolHelper.Parse(protocol);

            Assert.NotNull(result);
            Assert.Equal(result.Server, string.Empty);
            Assert.Equal(result.Username, "username");
        }

        [Fact]
        public void EmptyUsernameAndServerReturnsValidResult()
        {
            string protocol = "wallabag://@";
            var result = ProtocolHelper.Parse(protocol);

            Assert.NotNull(result);
            Assert.Equal(result.Server, string.Empty);
            Assert.Equal(result.Username, string.Empty);
        }
    }
}