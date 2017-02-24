using FakeItEasy;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using wallabag.Api;
using wallabag.Api.Models;
using wallabag.Api.Responses;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Interfaces;
using wallabag.Data.Models;
using wallabag.Data.Services;
using wallabag.Data.ViewModels;
using Xunit;

namespace wallabag.Tests
{
    public class LoginPageViewModelTests
    {
        [Fact]
        public void InvokingTheRegisterCommandOpensTheBrowser()
        {
            var logging = A.Fake<ILoggingService>();
            var navigation = A.Fake<INavigationService>();
            var device = A.Fake<IPlatformSpecific>();
            var client = A.Fake<IWallabagClient>();
            var apiService = A.Fake<IApiClientCreationService>();
            var database = TestsHelper.CreateFakeDatabase();

            var uriToTest = new Uri("https://test.de");

            var viewModel = new LoginPageViewModel(logging, navigation, device, client, apiService, database)
            {
                SelectedProvider = new Data.Models.WallabagProvider(uriToTest, "My test provider")
            };

            Assert.True(viewModel.RegisterCommand.CanExecute(null));

            viewModel.RegisterCommand.Execute();

            A.CallTo(() => device.LaunchUri(A<Uri>.That.IsEqualTo(uriToTest.Append("/register")), A<Uri>.Ignored)).MustHaveHappened();
        }

        [Fact]
        public void RegisterCommandCanNotBeExecutedIfSelectedProviderIsNull()
        {
            var logging = A.Fake<ILoggingService>();
            var navigation = A.Fake<INavigationService>();
            var device = A.Fake<IPlatformSpecific>();
            var client = A.Fake<IWallabagClient>();
            var apiService = A.Fake<IApiClientCreationService>();
            var database = TestsHelper.CreateFakeDatabase();

            var uriToTest = new Uri("https://test.de");

            var viewModel = new LoginPageViewModel(logging, navigation, device, client, apiService, database)
            {
                SelectedProvider = null
            };

            Assert.False(viewModel.RegisterCommand.CanExecute(null));
            A.CallTo(() => device.LaunchUri(A<Uri>.That.IsEqualTo(uriToTest.Append("/register")), A<Uri>.Ignored)).MustNotHaveHappened();
        }

        [Fact]
        public void InvokingTheScanQRCodeCommandNavigatesToTheQRPage()
        {
            var logging = A.Fake<ILoggingService>();
            var navigation = A.Fake<INavigationService>();
            var device = A.Fake<IPlatformSpecific>();
            var client = A.Fake<IWallabagClient>();
            var apiService = A.Fake<IApiClientCreationService>();
            var database = TestsHelper.CreateFakeDatabase();

            var viewModel = new LoginPageViewModel(logging, navigation, device, client, apiService, database);

            viewModel.ScanQRCodeCommand.Execute();

            A.CallTo(() => navigation.Navigate(Data.Common.Navigation.Pages.QRScanPage)).MustHaveHappened();
        }

        [Fact]
        public void RedirectUriReturnsNullIfApiVersionIsNewer()
        {
            var logging = A.Fake<ILoggingService>();
            var device = A.Fake<IPlatformSpecific>();
            var client = A.Fake<IWallabagClient>();

            var uriToTest = new Uri("https://test.de");
            var apiService = new ApiClientCreationService(logging, client, device);

            Assert.Null(apiService.GetRedirectUri(uriToTest, true));
        }

        [Fact]
        public void RedirectUriReturnsNullIfApiVersionIsOld()
        {
            var logging = A.Fake<ILoggingService>();
            var device = A.Fake<IPlatformSpecific>();
            var client = A.Fake<IWallabagClient>();

            var uriToTest = new Uri("https://test.de");
            var apiService = new ApiClientCreationService(logging, client, device);

            var redirectUri = apiService.GetRedirectUri(uriToTest, false);

            Assert.NotNull(redirectUri);
            Assert.True(uriToTest.IsBaseOf(redirectUri));

            A.CallTo(() => device.DeviceName).MustHaveHappened();
        }

        /* TODO
        [Fact]
        public async Task AfterConfigurationItemsAndTagsAreSavedInTheDatabase()
        {
            var logging = A.Fake<ILoggingService>();
            var navigation = A.Fake<INavigationService>();
            var device = A.Fake<IPlatformSpecific>();
            var client = A.Fake<IWallabagClient>();
            var apiService = A.Fake<IApiClientCreationService>();
            var database = TestsHelper.CreateFakeDatabase();
            var viewModel = new LoginPageViewModel(logging, navigation, device, client, apiService, database);

            A.CallTo(() => client.GetItemsWithEnhancedMetadataAsync(
                A<bool?>.Ignored,
                A<bool?>.Ignored,
                A<WallabagClient.WallabagDateOrder?>.Ignored,
                A<WallabagClient.WallabagSortOrder?>.Ignored,
                A<int?>.Ignored,
                A<int?>.Ignored,
                A<DateTime?>.Ignored,
                A<IEnumerable<string>>.Ignored,
                A<CancellationToken>.Ignored))
                .Returns(new ItemCollectionResponse()
                {
                    Page = 1,
                    Pages = 1,
                    Items = new List<WallabagItem>()
                    {
                        new WallabagItem()
                        {
                            Content = string.Empty,
                            CreationDate = DateTime.Now,
                            DomainName = "test.de",
                            EstimatedReadingTime = 10,
                            Id = 1,
                            IsRead = false,
                            IsStarred = false,
                            Language = "de-DE",
                            LastUpdated = DateTime.Now,
                            Mimetype = "text/html",
                            Tags = new List<WallabagTag>(),
                            Title = "My title",
                            Url = "https://test.de"
                        }
                    },
                    Limit = 1,
                    TotalNumberOfItems = 1
                });

            A.CallTo(() => client.GetTagsAsync(A<CancellationToken>.Ignored))
                .Returns(new List<WallabagTag>()
                {
                    new WallabagTag()
                    {
                        Id = 1,
                        Label = "test",
                        Slug = "test"
                    }
                });

            await viewModel.DownloadAndSaveItemsAndTagsAsync();

            A.CallTo(() => client.GetItemsWithEnhancedMetadataAsync(
                A<bool>.Ignored,
                A<bool>.Ignored,
                A<WallabagClient.WallabagDateOrder>.Ignored,
                A<WallabagClient.WallabagSortOrder>.Ignored,
                A<int>.Ignored,
                A<int>.Ignored,
                A<DateTime>.Ignored,
                A<IEnumerable<string>>.Ignored,
                A<CancellationToken>.Ignored)).MustHaveHappened();
            A.CallTo(() => client.GetTagsAsync(A<CancellationToken>.Ignored)).MustHaveHappened();

            Assert.Equal(1, database.ExecuteScalar<int>("select count(*) from Item"));
            Assert.Equal(1, database.ExecuteScalar<int>("select count(*) from Tag"));
        } */

        [Fact]
        public async Task NavigationWithParameterSetsProperties()
        {
            var logging = A.Fake<ILoggingService>();
            var navigation = A.Fake<INavigationService>();
            var device = A.Fake<IPlatformSpecific>();
            var client = A.Fake<IWallabagClient>();
            var apiService = A.Fake<IApiClientCreationService>();
            var database = TestsHelper.CreateFakeDatabase();
            var viewModel = new LoginPageViewModel(logging, navigation, device, client, apiService, database);

            var param = new ProtocolSetupNavigationParameter("user", "http://test.de");

            await viewModel.OnNavigatedToAsync(param, new Dictionary<string, object>());

            Assert.Equal(WallabagProvider.Other, viewModel.SelectedProvider);
            Assert.Equal("user", viewModel.Username);
            Assert.Equal("http://test.de", viewModel.Url);
        }

        [Fact]
        public void TheUrlFieldIsInvisibleIfTheSelectedProviderUrlIsNotNull()
        {
            var logging = A.Fake<ILoggingService>();
            var navigation = A.Fake<INavigationService>();
            var device = A.Fake<IPlatformSpecific>();
            var client = A.Fake<IWallabagClient>();
            var apiService = A.Fake<IApiClientCreationService>();
            var database = TestsHelper.CreateFakeDatabase();
            var viewModel = new LoginPageViewModel(logging, navigation, device, client, apiService, database)
            {
                SelectedProvider = new WallabagProvider(new Uri("https://test.de"), "My provider")
            };

            viewModel.RaisePropertyChanged(nameof(viewModel.SelectedProvider));

            Assert.NotNull(viewModel.SelectedProvider.Url);
            Assert.False(viewModel.UrlFieldIsVisible);
        }

        [Fact]
        public async Task ClientIsCreatedIfTheClientCredentialsAreUnset()
        {
            var logging = A.Fake<ILoggingService>();
            var navigation = A.Fake<INavigationService>();
            var device = A.Fake<IPlatformSpecific>();
            var client = A.Fake<IWallabagClient>();
            var apiService = A.Fake<IApiClientCreationService>();
            var database = TestsHelper.CreateFakeDatabase();
            var viewModel = new LoginPageViewModel(logging, navigation, device, client, apiService, database)
            {
                Url = "https://test.de",
                Username = "myuser",
                Password = "password"
            };

            Assert.True(string.IsNullOrEmpty(viewModel.ClientId));
            Assert.True(string.IsNullOrEmpty(viewModel.ClientSecret));

            await viewModel.TestConfigurationAsync();

            A.CallTo(() => apiService.CreateClientAsync(A<string>.Ignored, A<string>.That.IsEqualTo("myuser"), A<string>.That.IsEqualTo("password"))).MustHaveHappened();
        }

        [Fact]
        public async Task ClientIsCreatedIfTheClientCredentialsAreUnsetEvenIfTheUserCheckedTheCustomSettings()
        {
            var logging = A.Fake<ILoggingService>();
            var navigation = A.Fake<INavigationService>();
            var device = A.Fake<IPlatformSpecific>();
            var client = A.Fake<IWallabagClient>();
            var apiService = A.Fake<IApiClientCreationService>();
            var database = TestsHelper.CreateFakeDatabase();
            var viewModel = new LoginPageViewModel(logging, navigation, device, client, apiService, database)
            {
                Url = "https://test.de",
                Username = "myuser",
                Password = "password",
                UseCustomSettings = true
            };

            Assert.True(string.IsNullOrEmpty(viewModel.ClientId));
            Assert.True(string.IsNullOrEmpty(viewModel.ClientSecret));

            await viewModel.TestConfigurationAsync();

            A.CallTo(() => apiService.CreateClientAsync(A<string>.Ignored, A<string>.That.IsEqualTo("myuser"), A<string>.That.IsEqualTo("password"))).MustHaveHappened();
        }

        /* TODO
        [Fact]
        public async Task IfClientCreationWasSuccessfulThenDownloadsAreWorkingFine()
        {
            var logging = A.Fake<ILoggingService>();
            var navigation = A.Fake<INavigationService>();
            var device = A.Fake<IPlatformSpecific>();
            var client = A.Fake<IWallabagClient>();
            var apiService = A.Fake<IApiClientCreationService>();
            var database = TestsHelper.CreateFakeDatabase();
            var viewModel = new LoginPageViewModel(logging, navigation, device, client, apiService, database)
            {
                Url = "https://test.de",
                Username = "myuser",
                Password = "password"
            };

            Assert.True(string.IsNullOrEmpty(viewModel.ClientId));
            Assert.True(string.IsNullOrEmpty(viewModel.ClientSecret));

            A.CallTo(() => apiService.CreateClientAsync(
                A<string>.Ignored,
                A<string>.That.IsEqualTo("myuser"),
                A<string>.That.IsEqualTo("password")))
                .Returns(new ClientCreationData()
                {
                    Id = "myId",
                    Name = "test",
                    Secret = "secret"
                });
            A.CallTo(() => client.RequestTokenAsync(
                A<string>.That.IsEqualTo("myuser"),
                A<string>.That.IsEqualTo("password"),
                A<CancellationToken>.Ignored))
                .Returns(true);

            bool configurationIsValid = await viewModel.TestConfigurationAsync();
            Assert.True(configurationIsValid);

            await viewModel.DownloadAndSaveItemsAndTagsAsync();

            A.CallTo(() => apiService.CreateClientAsync(A<string>.Ignored, A<string>.That.IsEqualTo("myuser"), A<string>.That.IsEqualTo("password"))).MustHaveHappened();
            A.CallTo(() => client.GetItemsWithEnhancedMetadataAsync(
                A<bool?>.Ignored,
                A<bool?>.Ignored,
                A<WallabagClient.WallabagDateOrder>.Ignored,
                A<WallabagClient.WallabagSortOrder>.Ignored,
                A<int?>.Ignored,
                A<int?>.Ignored,
                A<DateTime?>.Ignored,
                A<IEnumerable<string>>.Ignored,
                A<CancellationToken>.Ignored)).MustHaveHappened();
            A.CallTo(() => client.GetTagsAsync(A<CancellationToken>.Ignored)).MustHaveHappened();
        }*/
    }
}
