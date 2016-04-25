﻿using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Threading.Tasks;
using static wallabag.Api.Tests.Credentials;

namespace wallabag.Api.Tests
{
    public partial class GeneralTests
    {
        [TestMethod]
        [TestCategory("Authentication")]
        public void RefreshingTokenFailsIfThereIsNoRefreshToken()
        {
            client.RefreshToken = string.Empty;
            Assert.ThrowsException<Exception>(async () => await client.RefreshAccessTokenAsync());
        }

        [TestMethod]
        [TestCategory("Authentication")]
        public async Task RequestTokenSetsAccessAndRefreshToken()
        {
            await client.RequestTokenAsync(username, password);
            Assert.IsTrue(!string.IsNullOrEmpty(client.AccessToken));
            Assert.IsTrue(!string.IsNullOrEmpty(client.RefreshToken));
        }

        [TestMethod]
        [TestCategory("Authentication")]
        public void RequestTokenFailsWithoutCredentials()
        {
            Assert.ThrowsException<ArgumentNullException>(async () => await client.RequestTokenAsync(string.Empty, string.Empty));
            Assert.ThrowsException<ArgumentNullException>(async () => await client.RequestTokenAsync("username", string.Empty));
            Assert.ThrowsException<ArgumentNullException>(async () => await client.RequestTokenAsync(string.Empty, "password"));
        }
    }
}