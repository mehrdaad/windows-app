﻿using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Threading.Tasks;
using static wallabag.Api.Tests.Credentials;

namespace wallabag.Api.Tests
{
    [TestClass]
    public partial class GeneralTests
    {
        WallabagClient client;

        [TestInitialize]
        public async Task InitializeUnitTests()
        {
            client = new WallabagClient(new Uri(wallabagUrl), clientId, clientSecret);
            await client.RequestTokenAsync(username, password);
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            var items = await client.GetItemsAsync();
            foreach (var item in items)
                await client.DeleteAsync(item);
        }

        [TestMethod]
        [TestCategory("General")]
        public async Task VersionNumberReturnsValidValue()
        {
            var version = await client.GetVersionNumberAsync();
            Assert.IsTrue(version.Contains("2.0"));
        }

        [TestMethod]
        [TestCategory("General")]
        public void InitializationFailsWithInvalidUri()
        {
            Assert.ThrowsException<UriFormatException>(() =>
            {
                var test = new WallabagClient(new Uri(""), clientId, clientSecret);
            });
        }
    }
}