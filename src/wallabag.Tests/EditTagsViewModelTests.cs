using FakeItEasy;
using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Data.Models;
using wallabag.Data.Services;
using wallabag.Data.Services.OfflineTaskService;
using wallabag.Data.ViewModels;
using Xunit;

namespace wallabag.Tests
{
    public class EditTagsViewModelTests
    {
        [Fact]
        public void CancellingWillNavigateBack()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            var viewModel = new EditTagsViewModel(offlineTaskService, loggingService, database, navigationService);

            viewModel.CancelCommand.Execute(null);

            A.CallTo(() => navigationService.GoBack()).MustHaveHappened();
        }

        [Fact]
        public void InvokingTheQueryUpdateUpdatesTheSuggestions()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            //Fake some tags
            var fakeTags = new List<Tag>();
            for (int i = 0; i < 3; i++)
            {
                var tag = new Tag()
                {
                    Label = $"{nameof(EditTagsViewModelTests)} fake tag {i}",
                    Id = i
                };

                fakeTags.Add(tag);
                database.Insert(tag);
            }

            var viewModel = new EditTagsViewModel(offlineTaskService, loggingService, database, navigationService)
            {
                TagQuery = nameof(EditTagsViewModelTests)
            };
            viewModel.TagQueryChangedCommand.Execute(null);

            Assert.Equal(3, viewModel.Suggestions.Count);
            Assert.Contains(nameof(EditTagsViewModel), viewModel.Suggestions[0].Label);
        }

        [Fact]
        public void SubmittingASuggestionAddsItToTheTagsList()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            var viewModel = new EditTagsViewModel(offlineTaskService, loggingService, database, navigationService);
            var tagToTest = new Tag()
            {
                Id = 1337,
                Label = "My suggestion tag"
            };

            viewModel.TagSubmittedCommand.Execute(tagToTest);

            Assert.Equal(1, viewModel.Tags.Count);
            Assert.Equal(tagToTest, viewModel.Tags[0]);
        }

        [Fact]
        public void SubmittingADuplicateDoesNotAddItToTheTagList()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            var viewModel = new EditTagsViewModel(offlineTaskService, loggingService, database, navigationService);
            var tagToTest = new Tag()
            {
                Id = 1337,
                Label = "My suggestion tag"
            };

            viewModel.TagSubmittedCommand.Execute(tagToTest);
            Assert.Equal(1, viewModel.Tags.Count);
            Assert.Equal(tagToTest, viewModel.Tags[0]);

            viewModel.TagSubmittedCommand.Execute(tagToTest);
            Assert.Equal(1, viewModel.Tags.Count);
            Assert.Equal(tagToTest, viewModel.Tags[0]);
        }

        [Fact]
        public void SubmittingNothingParsesTheCurrentTagQuery()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            var viewModel = new EditTagsViewModel(offlineTaskService, loggingService, database, navigationService)
            {
                TagQuery = "test1,test2,test3"
            };

            viewModel.TagSubmittedCommand.Execute(null);
            Assert.Equal(3, viewModel.Tags.Count);

            foreach (var tag in viewModel.Tags)
                Assert.Matches("test[1-3]", tag.Label);

            Assert.False(viewModel.TagsCountIsZero);
        }

        [Fact]
        public void SubmittingNothingParsesTheCurrentTagQueryWithoutDuplicates()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            var viewModel = new EditTagsViewModel(offlineTaskService, loggingService, database, navigationService)
            {
                TagQuery = "test1,test2,test2"
            };

            viewModel.TagSubmittedCommand.Execute(null);
            Assert.Equal(2, viewModel.Tags.Count);

            foreach (var tag in viewModel.Tags)
                Assert.Matches("test[1-2]", tag.Label);

            Assert.False(viewModel.TagsCountIsZero);
        }

        [Fact]
        public void SubmittingWillClearTheQueryAfterwards()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            var viewModel = new EditTagsViewModel(offlineTaskService, loggingService, database, navigationService)
            {
                TagQuery = "test1"
            };

            viewModel.TagSubmittedCommand.Execute(null);
            Assert.Equal(1, viewModel.Tags.Count);

            Assert.False(viewModel.TagsCountIsZero);
            Assert.True(string.IsNullOrEmpty(viewModel.TagQuery));
        }

        [Fact]
        public void AddingMultipleItemsWithTagsDoesntAddThemToTheCurrentTagList()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            var viewModel = new EditTagsViewModel(offlineTaskService, loggingService, database, navigationService)
            {
                Items = new List<Item>()
                {
                    new Item() { Id = 1, Title = "First test" },
                    new Item() { Id = 2, Title = "Second test" },
                }
            };

            Assert.Equal(0, viewModel.Tags.Count);
            Assert.True(viewModel.TagsCountIsZero);
        }

        [Fact]
        public async Task AddingOneItemWithTagsDoesAddThemToTheCurrentTagList()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            var item = new Item()
            {
                Id = 1,
                Tags = new System.Collections.ObjectModel.ObservableCollection<Tag>()
                {
                    new Tag() { Id = 1, Label = "test" },
                    new Tag() { Id = 2, Label = "test" },
                    new Tag() { Id = 3, Label = "test" },
                }
            };

            database.Insert(item);

            var viewModel = new EditTagsViewModel(offlineTaskService, loggingService, database, navigationService);
            await viewModel.OnNavigatedToAsync(item.Id, new Dictionary<string, object>(), Data.Common.NavigationMode.New);

            Assert.Equal(3, viewModel.Tags.Count);
            Assert.False(viewModel.TagsCountIsZero);
        }

        [Fact]
        public void AddingATagToMultipleItemsAddsThemUsingTheOfflineTaskService()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            var viewModel = new EditTagsViewModel(offlineTaskService, loggingService, database, navigationService)
            {
                Items = new List<Item>()
                {
                    new Item() { Id = 1, Title = "First test" },
                    new Item() { Id = 2, Title = "Second test" },
                }
            };

            viewModel.Tags.Add(new Tag() { Id = 1, Label = "test" });
            viewModel.FinishCommand.Execute(null);

            A.CallTo(() => offlineTaskService.Add(A<int>.Ignored, OfflineTask.OfflineTaskAction.EditTags, A<List<Tag>>.Ignored, A<List<Tag>>.Ignored)).MustHaveHappened(Repeated.Exactly.Twice);
        }

        [Fact]
        public async Task RemovingAnTagAndAddingAnotherExecutesTheProperActions()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            var tagToRemove = new Tag() { Id = 2, Label = "test" };
            var tagToAdd = new Tag() { Id = 4, Label = "test" };
            var item = new Item()
            {
                Id = 1,
                Tags = new System.Collections.ObjectModel.ObservableCollection<Tag>()
                {
                    new Tag() { Id = 1, Label = "test" },
                    tagToRemove,
                    new Tag() { Id = 3, Label = "test" },
                }
            };
            database.Insert(item);

            var viewModel = new EditTagsViewModel(offlineTaskService, loggingService, database, navigationService);
            await viewModel.OnNavigatedToAsync(item.Id, new Dictionary<string, object>(), Data.Common.NavigationMode.New);

            Assert.Equal(3, viewModel.Tags.Count);

            viewModel.Tags.Add(tagToAdd);
            viewModel.Tags.Remove(tagToRemove);

            Assert.Equal(3, viewModel.Tags.Count);

            viewModel.FinishCommand.Execute(null);

            A.CallTo(() => offlineTaskService.Add(A<int>.Ignored, OfflineTask.OfflineTaskAction.EditTags, A<List<Tag>>.That.Contains(tagToAdd), A<List<Tag>>.That.Contains(tagToRemove))).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => navigationService.GoBack()).MustHaveHappened();
        }

        [Fact]
        public void EnteringATagQueryOnlyRespectsTheLastString()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            // Fake some tags
            database.Insert(new Tag() { Id = 0, Label = "random tag" });

            var viewModel = new EditTagsViewModel(offlineTaskService, loggingService, database, navigationService)
            {
                TagQuery = "test1,test2,random"
            };
            viewModel.TagQueryChangedCommand.Execute(null);

            Assert.Equal(1, viewModel.Suggestions.Count);
            Assert.Equal("random tag", viewModel.Suggestions[0].Label);
        }

        [Fact]
        public void ParsingTheTagQueryWontAddEmptyOrSpaceStrings()
        {
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();
            var navigationService = A.Fake<INavigationService>();

            var viewModel = new EditTagsViewModel(offlineTaskService, loggingService, database, navigationService)
            {
                TagQuery = ",  ,"
            };
            viewModel.TagSubmittedCommand.Execute(null);
            Assert.Equal(0, viewModel.Suggestions.Count);
        }
    }
}
