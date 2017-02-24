using FakeItEasy;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using wallabag.Api;
using wallabag.Api.Models;
using wallabag.Data.Interfaces;
using wallabag.Data.Models;
using wallabag.Data.Services;
using wallabag.Data.ViewModels;
using Xunit;

namespace wallabag.Tests
{
    public class ItemPageViewModelTests
    {
        [Fact]
        public async Task NavigationWithAWrongParameterReportsError()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var platform = A.Fake<IPlatformSpecific>();
            var navigationService = A.Fake<INavigationService>();
            var client = A.Fake<IWallabagClient>();
            var database = TestsHelper.CreateFakeDatabase();

            var viewModel = new ItemPageViewModel(offlineTaskService, loggingService, platform, navigationService, client, database);
            await viewModel.OnNavigatedToAsync(9999, new Dictionary<string, object>());

            Assert.True(viewModel.ErrorDuringInitialization);
            A.CallTo(() => platform.GetArticleTemplateAsync()).MustNotHaveHappened();
        }

        [Fact]
        public async Task NavigationWithAnEmptyArticleFails()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var platform = A.Fake<IPlatformSpecific>();
            var navigationService = A.Fake<INavigationService>();
            var client = A.Fake<IWallabagClient>();
            var database = TestsHelper.CreateFakeDatabase();

            var item = new Item()
            {
                Id = 1,
                Content = string.Empty
            };
            database.Insert(item);

            var viewModel = new ItemPageViewModel(offlineTaskService, loggingService, platform, navigationService, client, database);
            await viewModel.OnNavigatedToAsync(item.Id, new Dictionary<string, object>());

            Assert.True(viewModel.ErrorDuringInitialization);
            A.CallTo(() => platform.GetArticleTemplateAsync()).MustNotHaveHappened();
        }

        [Fact]
        public async Task ArticleWithEmptyContentIsReFetchedFromTheServerAndContinuesIfSuccessful()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var platform = A.Fake<IPlatformSpecific>();
            var navigationService = A.Fake<INavigationService>();
            var client = A.Fake<IWallabagClient>();
            var database = TestsHelper.CreateFakeDatabase();

            // Insert a sample item without content to the database
            var item = new Item()
            {
                Id = 1,
                Content = string.Empty
            };
            database.Insert(item);

            // If the ViewModel asks for an internet connection, return true
            A.CallTo(() => platform.InternetConnectionIsAvailable).Returns(true);

            // This is our custom template to see if the formatting was fine
            A.CallTo(() => platform.GetArticleTemplateAsync()).Returns("{{title}}");

            // Return an item if asked for
            A.CallTo(() => client.GetItemAsync(A<int>.That.IsEqualTo(1), A<CancellationToken>.Ignored)).Returns(new WallabagItem()
            {
                Id = 1,
                Content = "This is my content.",
                CreationDate = DateTime.Now,
                DomainName = "test.de",
                EstimatedReadingTime = 10,
                IsRead = false,
                IsStarred = false,
                Language = "de-DE",
                LastUpdated = DateTime.Now,
                Mimetype = "text/html",
                Title = "This is my title",
                Url = "https://test.de"
            });

            var viewModel = new ItemPageViewModel(offlineTaskService, loggingService, platform, navigationService, client, database);
            await viewModel.OnNavigatedToAsync(item.Id, new Dictionary<string, object>());

            // Everything should be fine and our custom template should be applied
            A.CallTo(() => platform.InternetConnectionIsAvailable).MustHaveHappened();
            A.CallTo(() => client.GetItemAsync(A<int>.That.IsEqualTo(1), A<CancellationToken>.Ignored)).MustHaveHappened();
            A.CallTo(() => platform.GetArticleTemplateAsync()).MustHaveHappened();
            Assert.False(viewModel.ErrorDuringInitialization);
            Assert.False(string.IsNullOrEmpty(viewModel.FormattedHtml));
            Assert.Equal("This is my title", viewModel.FormattedHtml);
        }

        [Fact]
        public async Task ArticleWithEmptyContentShouldFailWithoutAnInternetConnection()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var platform = A.Fake<IPlatformSpecific>();
            var navigationService = A.Fake<INavigationService>();
            var client = A.Fake<IWallabagClient>();
            var database = TestsHelper.CreateFakeDatabase();

            // Insert a sample item without content to the database
            var item = new Item()
            {
                Id = 1,
                Content = string.Empty
            };
            database.Insert(item);

            // If the ViewModel asks for an internet connection, return false
            A.CallTo(() => platform.InternetConnectionIsAvailable).Returns(false);

            var viewModel = new ItemPageViewModel(offlineTaskService, loggingService, platform, navigationService, client, database);
            await viewModel.OnNavigatedToAsync(item.Id, new Dictionary<string, object>());

            A.CallTo(() => platform.InternetConnectionIsAvailable).MustHaveHappened();
            A.CallTo(() => client.GetItemAsync(A<int>.That.IsEqualTo(1), A<CancellationToken>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => platform.GetArticleTemplateAsync()).MustNotHaveHappened();
            Assert.True(viewModel.ErrorDuringInitialization);
            Assert.True(string.IsNullOrEmpty(viewModel?.FormattedHtml));
        }

        [Fact]
        public void ExecutingTheChangeReadStatusCommandChangesItemPropertiesCorrect()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var platform = A.Fake<IPlatformSpecific>();
            var navigationService = A.Fake<INavigationService>();
            var client = A.Fake<IWallabagClient>();
            var database = TestsHelper.CreateFakeDatabase();

            var item = new Item()
            {
                Id = 1,
                Content = string.Empty,
                IsRead = false
            };

            var viewModel = new ItemPageViewModel(offlineTaskService, loggingService, platform, navigationService, client, database)
            {
                Item = new ItemViewModel(item, offlineTaskService, navigationService, loggingService, platform, database)
            };

            Assert.False(viewModel.Item.Model.IsRead);

            viewModel.ChangeReadStatusCommand.Execute(null);
            Assert.True(viewModel.Item.Model.IsRead);

            viewModel.ChangeReadStatusCommand.Execute(null);
            Assert.False(viewModel.Item.Model.IsRead);
        }

        [Fact]
        public void ExecutingTheChangeFavoriteStatusCommandChangesItemPropertiesCorrect()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var platform = A.Fake<IPlatformSpecific>();
            var navigationService = A.Fake<INavigationService>();
            var client = A.Fake<IWallabagClient>();
            var database = TestsHelper.CreateFakeDatabase();

            var item = new Item()
            {
                Id = 1,
                Content = string.Empty,
                IsStarred = false
            };

            var viewModel = new ItemPageViewModel(offlineTaskService, loggingService, platform, navigationService, client, database)
            {
                Item = new ItemViewModel(item, offlineTaskService, navigationService, loggingService, platform, database)
            };
            Assert.False(viewModel.Item.Model.IsStarred);

            viewModel.ChangeFavoriteStatusCommand.Execute(null);
            Assert.True(viewModel.Item.Model.IsStarred);

            viewModel.ChangeFavoriteStatusCommand.Execute(null);
            Assert.False(viewModel.Item.Model.IsStarred);
        }

        [Fact]
        public void EditingTheTagsNavigatesToProperPage()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var platform = A.Fake<IPlatformSpecific>();
            var navigationService = A.Fake<INavigationService>();
            var client = A.Fake<IWallabagClient>();
            var database = TestsHelper.CreateFakeDatabase();

            var item = new Item()
            {
                Id = 1,
                Tags = new System.Collections.ObjectModel.ObservableCollection<Tag>()
                {
                    new Tag() { Label = "test" }
                }
            };

            var viewModel = new ItemPageViewModel(offlineTaskService, loggingService, platform, navigationService, client, database)
            {
                Item = new ItemViewModel(item, offlineTaskService, navigationService, loggingService, platform, database)
            };

            viewModel.EditTagsCommand.Execute(null);

            A.CallTo(() => navigationService.Navigate(A<Data.Common.Navigation.Pages>.That.IsEqualTo(Data.Common.Navigation.Pages.EditTagsPage), A<object>.Ignored)).MustHaveHappened();
        }

        [Fact]
        public void DeletingAnItemDeletesTheItemAndNavigatesBack()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var platform = A.Fake<IPlatformSpecific>();
            var navigationService = A.Fake<INavigationService>();
            var client = A.Fake<IWallabagClient>();
            var database = TestsHelper.CreateFakeDatabase();

            var item = new Item() { Id = 1 };

            var fakeItemViewModel = A.Fake<ItemViewModel>(x => x.WithArgumentsForConstructor(() => new ItemViewModel(item, offlineTaskService, navigationService, loggingService, platform, database)));
            var viewModel = new ItemPageViewModel(offlineTaskService, loggingService, platform, navigationService, client, database)
            {
                Item = fakeItemViewModel
            };

            viewModel.DeleteCommand.Execute(null);

            A.CallTo(() => navigationService.GoBack()).MustHaveHappened();
        }

        [Fact]
        public async Task UsingYoutubeOrVimeoAsHostnameSetsTheHostnameEmpty()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var platform = A.Fake<IPlatformSpecific>();
            var navigationService = A.Fake<INavigationService>();
            var client = A.Fake<IWallabagClient>();
            var database = TestsHelper.CreateFakeDatabase();

            var fakeItems = new List<Item>
            {
                new Item()
                {
                    Id = 1,
                    Hostname = "youtube.com",
                    Content = "test content",
                    PreviewImageUri = new Uri("https://test.de")
                },
                new Item()
                {
                    Id = 2,
                    Hostname = "vimeo.com",
                    Content = "test content",
                    PreviewImageUri = new Uri("https://test.de")
                }
            };

            A.CallTo(() => platform.GetArticleTemplateAsync()).Returns("{{imageHeader}}");

            foreach (var item in fakeItems)
            {
                database.Insert(item);

                var fakeItemViewModel = new ItemViewModel(item, offlineTaskService, navigationService, loggingService, platform, database);
                var viewModel = new ItemPageViewModel(offlineTaskService, loggingService, platform, navigationService, client, database)
                {
                    Item = fakeItemViewModel
                };
                await viewModel.OnNavigatedToAsync(item.Id, new Dictionary<string, object>());

                Assert.False(viewModel.ErrorDuringInitialization);
                Assert.Equal(string.Empty, viewModel.FormattedHtml);
            }
        }

        [Theory]
        [InlineData("youtube", "lp00DMy3aVw")]
        [InlineData("youtube", "RhU9MZ98jxo")]
        [InlineData("vimeo", "192147860")]
        public async Task GettingAPreviewImageForAVideoReturnsValidValue(string provider, string videoId)
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var platform = A.Fake<IPlatformSpecific>();
            var navigationService = A.Fake<INavigationService>();
            var client = A.Fake<IWallabagClient>();
            var database = TestsHelper.CreateFakeDatabase();

            var viewModel = new ItemPageViewModel(offlineTaskService, loggingService, platform, navigationService, client, database);
            string result = await viewModel.GetPreviewImageForVideoAsync(provider, videoId);
            Assert.NotEqual(string.Empty, result);
            Assert.True(result.StartsWith("http"));
        }

        [Fact]
        public void SavingARightClickUriExecutesTheProperActions()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var platform = A.Fake<IPlatformSpecific>();
            var navigationService = A.Fake<INavigationService>();
            var client = A.Fake<IWallabagClient>();
            var database = TestsHelper.CreateFakeDatabase();

            A.CallTo(() => platform.InternetConnectionIsAvailable).Returns(true);

            var viewModel = new ItemPageViewModel(offlineTaskService, loggingService, platform, navigationService, client, database)
            {
                RightClickUri = new Uri("https://google.de")
            };

            viewModel.SaveRightClickLinkCommand.Execute(null);

            A.CallTo(() => offlineTaskService.Add(A<string>.Ignored, A<IEnumerable<string>>.Ignored)).MustHaveHappened();
        }

        [Fact]
        public void OpeningARightClickUriExecutesTheProperActions()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var platform = A.Fake<IPlatformSpecific>();
            var navigationService = A.Fake<INavigationService>();
            var client = A.Fake<IWallabagClient>();
            var database = TestsHelper.CreateFakeDatabase();

            A.CallTo(() => platform.InternetConnectionIsAvailable).Returns(true);

            var viewModel = new ItemPageViewModel(offlineTaskService, loggingService, platform, navigationService, client, database)
            {
                RightClickUri = new Uri("https://google.de")
            };

            viewModel.OpenRightClickLinkInBrowserCommand.Execute(null);

            A.CallTo(() => platform.LaunchUri(A<Uri>.That.IsEqualTo(viewModel.RightClickUri), A<Uri>.Ignored)).MustHaveHappened();
        }

        [Theory]
        [InlineData("<iframe src='https://player.vimeo.com/video/203689226?byline=0&portrait=0' width='640' height='360' frameborder='0' webkitallowfullscreen mozallowfullscreen allowfullscreen></iframe>", "http://i.vimeocdn.com/video/618033580_640.jpg")]
        [InlineData("<iframe src='https://player.vimeo.com/video/192147860' width='640' height='360' frameborder='0' webkitallowfullscreen mozallowfullscreen allowfullscreen></iframe>", "http://i.vimeocdn.com/video/603421417_640.jpg")]
        [InlineData("<iframe width='560' height='315' src='https://www.youtube.com/embed/lp00DMy3aVw' frameborder='0' allowfullscreen></iframe>", "http://img.youtube.com/vi/lp00DMy3aVw/0.jpg")]
        [InlineData("<iframe width='560' height='315' src='https://www.youtube.com/embed/v2H4l9RpkwM' frameborder='0' allowfullscreen></iframe>", "http://img.youtube.com/vi/v2H4l9RpkwM/0.jpg")]
        public async Task VideoPreviewImageIsCorrectlyFetchedFromIFrames(string iframe, string expectedResult)
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var platform = A.Fake<IPlatformSpecific>();
            var navigationService = A.Fake<INavigationService>();
            var client = A.Fake<IWallabagClient>();
            var database = TestsHelper.CreateFakeDatabase();

            var fakeItem = new Item()
            {
                Id = 1,
                Hostname = "wallabag.it",
                Content = iframe,
                CreationDate = DateTime.Now,
                LastModificationDate = DateTime.Now,
                EstimatedReadingTime = 10,
                IsRead = false,
                IsStarred = false,
                Language = "de-DE",
                Mimetype = "text/html",
                ReadingProgress = 0,
                Title = "My title",
                Url = "https://wallabag.it",
                PreviewImageUri = new Uri("https://test.de")
            };

            A.CallTo(() => platform.GetArticleTemplateAsync()).Returns("{{content}}");

            database.Insert(fakeItem);

            var fakeItemViewModel = new ItemViewModel(fakeItem, offlineTaskService, navigationService, loggingService, platform, database);
            var viewModel = new ItemPageViewModel(offlineTaskService, loggingService, platform, navigationService, client, database)
            {
                Item = fakeItemViewModel
            };
            await viewModel.OnNavigatedToAsync(fakeItem.Id, new Dictionary<string, object>());

            Assert.False(viewModel.ErrorDuringInitialization);
            Assert.Contains(expectedResult, viewModel.FormattedHtml);
        }
    }
}