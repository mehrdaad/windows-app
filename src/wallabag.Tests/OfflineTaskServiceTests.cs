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
using Xunit;

namespace wallabag.Tests
{
    public class OfflineTaskServiceTests
    {
        [Fact]
        public void AddingATaskExecutesItDirectly()
        {
            string uriString = "https://wallabag.org";
            var uriToTest = new Uri(uriString);

            var client = A.Fake<IWallabagClient>();
            var platform = A.Fake<IPlatformSpecific>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();

            A.CallTo(() => platform.InternetConnectionIsAvailable).Returns(true);
            A.CallTo(() => client.AddAsync(A<Uri>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).Returns(new WallabagItem()
            {
                Id = 1,
                Title = "My fake item",
                Url = uriString,
                Tags = new List<WallabagTag>()
            });

            var taskService = new OfflineTaskService(client, database, loggingService, platform);
            taskService.Add(uriString, new List<string>());

            A.CallTo(() => client.AddAsync(uriToTest, A<IEnumerable<string>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).MustHaveHappened();
        }

        [Fact]
        public void ExecutingAnOfflineTaskWithoutInternetConnectionDoesNotCallTheAPI()
        {
            var client = A.Fake<IWallabagClient>();
            var platform = A.Fake<IPlatformSpecific>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();

            A.CallTo(() => platform.InternetConnectionIsAvailable).Returns(false);

            var taskService = new OfflineTaskService(client, database, loggingService, platform);
            taskService.Add("http://test.de", new List<string>());

            A.CallTo(() => client.AddAsync(A<Uri>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).MustNotHaveHappened();
        }

        [Fact]
        public async Task ExecutingAQueueOfTasksActuallyExecutesEachOfThem()
        {
            var client = A.Fake<IWallabagClient>();
            var platform = A.Fake<IPlatformSpecific>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();

            A.CallTo(() => platform.InternetConnectionIsAvailable).Returns(false);

            var taskService = new OfflineTaskService(client, database, loggingService, platform);

            for (int i = 0; i < 10; i++)
            {
                taskService.Tasks.Add(new OfflineTask()
                {
                    Id = i,
                    ItemId = i,
                    Action = OfflineTask.OfflineTaskAction.AddItem,
                    Url = "https://wallabag.it"
                });
            }

            A.CallTo(() => platform.InternetConnectionIsAvailable).Returns(true);
            A.CallTo(() => client.AddAsync(A<Uri>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).Returns(new WallabagItem()
            {
                Id = 1,
                Title = "My fake item",
                Url = "https://test.de",
                Tags = new List<WallabagTag>()
            });

            await taskService.ExecuteAllAsync();

            A.CallTo(() => client.AddAsync(A<Uri>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).MustHaveHappened(Repeated.Exactly.Times(10));
        }

        [Fact]
        public void HavingItemsInTheDatabaseAtStartShouldIncludeThemOnInit()
        {
            var client = A.Fake<IWallabagClient>();
            var platform = A.Fake<IPlatformSpecific>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();

            for (int i = 0; i < 3; i++)
                database.Insert(new OfflineTask() { Id = i, ItemId = i });

            var taskService = new OfflineTaskService(client, database, loggingService, platform);

            Assert.Equal(database.ExecuteScalar<int>("select count(*) from OfflineTask"), taskService.Tasks.Count);
        }

        [Fact]
        public void ExecutingATaskWithFalseAPIEndpointDoesNotRemoveThemFromTheDatabase()
        {
            var client = A.Fake<IWallabagClient>();
            var platform = A.Fake<IPlatformSpecific>();
            var loggingService = A.Fake<ILoggingService>();
            var database = TestsHelper.CreateFakeDatabase();

            A.CallTo(() => client.ArchiveAsync(A<WallabagItem>.Ignored, A<CancellationToken>.Ignored)).Returns(false);

            var taskService = new OfflineTaskService(client, database, loggingService, platform);
            int count = taskService.Tasks.Count;
            var task = new OfflineTask()
            {
                Action = OfflineTask.OfflineTaskAction.MarkAsRead,
                ItemId = 0,
                Id = 0
            };
            taskService.Tasks.Add(task);

            Assert.Equal(count + 1, taskService.Tasks.Count);

            //TODO: Abstract the API layer of the database, so that even database interactions can be faked
            //A.CallTo(() => database.Delete<OfflineTask>(A<object>.Ignored)).MustNotHaveHappened();
        }
    }
}
