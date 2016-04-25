using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Threading.Tasks;
using static wallabag.Api.Tests.Credentials;

namespace wallabag.Api.Tests
{
    [TestClass]
    public class GeneralTests
    {
        WallabagClient client;

        [TestInitialize]
        public async Task InitializeUnitTests()
        {
            client = new WallabagClient(new Uri(wallabagUrl), clientId, clientSecret);
            await client.RequestTokenAsync(username, password);
        }

        [TestMethod]
        public async Task VersionNumberReturnsValidValue()
        {
            var version = await client.GetVersionNumberAsync();
            Assert.IsTrue(version.Contains("2.0"));
        }

        [TestMethod]
        public void InitializationFailsWithInvalidUri()
        {
            Assert.ThrowsException<UriFormatException>(() =>
            {
                var test = new WallabagClient(new Uri(""), clientId, clientSecret);
            });
        }
    }
}
