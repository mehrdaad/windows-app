using FakeItEasy;
using System;
using System.Collections.Generic;
using wallabag.Data.Interfaces;
using wallabag.Data.Models;
using wallabag.Data.Services;
using wallabag.Data.ViewModels;
using Xunit;

namespace wallabag.Tests
{
    public class ItemViewModelTests
    {
        [Fact]
        public void WhenModelIsUpdatedTheViewModelIsUpdatedToo()
        {
            var item = A.Fake<Item>();
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var navigation = A.Fake<INavigationService>();
            var logging = A.Fake<ILoggingService>();
            var device = A.Fake<IPlatformSpecific>();
            var database = TestsHelper.CreateFakeDatabase();

            var viewModel = new ItemViewModel(item,
                offlineTaskService,
                navigation,
                logging,
                device,
                database);

            bool eventWasFired = false;
            viewModel.PropertyChanged += (s, e) =>
            {
                eventWasFired = true;
                Assert.True(e.PropertyName == nameof(ItemViewModel.Model));
            };
            Assert.True(eventWasFired);
        }

        [Fact]
        public void WhenTagsAreAddedOrRemovedTheTagRelatedPropertiesAreGettingUpdated()
        {
            var item = A.Fake<Item>();
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var navigation = A.Fake<INavigationService>();
            var logging = A.Fake<ILoggingService>();
            var device = A.Fake<IPlatformSpecific>();
            var database = TestsHelper.CreateFakeDatabase();

            var viewModel = new ItemViewModel(item,
                offlineTaskService,
                navigation,
                logging,
                device,
                database);

            int eventCounter = 0;
            viewModel.PropertyChanged += (s, e) =>
            {
                eventCounter++;
                Assert.True((e.PropertyName == nameof(ItemViewModel.TagsString)) || (e.PropertyName == nameof(ItemViewModel.TagsAreExisting)));
            };

            viewModel.Model.Tags.Add(new Tag() { Label = "test" });

            Assert.Equal(2, eventCounter);
            Assert.True(viewModel.TagsAreExisting);
            Assert.Equal("test", viewModel.TagsString);
        }

        [Fact]
        public void MarkingAnItemAsReadInvokesProperActions()
        {
            var item = A.Fake<Item>(x => x.WithArgumentsForConstructor(() => new Item()
            {
                Id = 10,
                IsRead = false
            }));
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var navigation = A.Fake<INavigationService>();
            var logging = A.Fake<ILoggingService>();
            var device = A.Fake<IPlatformSpecific>();
            var database = TestsHelper.CreateFakeDatabase();

            database.Insert(item);

            var viewModel = new ItemViewModel(item,
                offlineTaskService,
                navigation,
                logging,
                device,
                database);

            bool propertyChangedEventWasFired = false;
            viewModel.PropertyChanged += (s, e) => propertyChangedEventWasFired = true;

            viewModel.MarkAsReadCommand.Execute(null);

            A.CallTo(() => offlineTaskService.Add(
                10,
                A<OfflineTask.OfflineTaskAction>.That.IsEqualTo(OfflineTask.OfflineTaskAction.MarkAsRead),
                A<List<Tag>>.Ignored,
                A<List<Tag>>.Ignored)).MustHaveHappened();
            Assert.True(propertyChangedEventWasFired);
            Assert.True(database.Get<Item>(10).IsRead);
        }

        [Fact]
        public void MarkingAnItemAsUnreadInvokesProperActions()
        {
            var item = A.Fake<Item>(x => x.WithArgumentsForConstructor(() => new Item()
            {
                Id = 10,
                IsRead = true
            }));
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var navigation = A.Fake<INavigationService>();
            var logging = A.Fake<ILoggingService>();
            var device = A.Fake<IPlatformSpecific>();
            var database = TestsHelper.CreateFakeDatabase();

            database.Insert(item);

            var viewModel = new ItemViewModel(item,
                offlineTaskService,
                navigation,
                logging,
                device,
                database);

            bool propertyChangedEventWasFired = false;
            viewModel.PropertyChanged += (s, e) => propertyChangedEventWasFired = true;

            viewModel.UnmarkAsReadCommand.Execute(null);

            A.CallTo(() => offlineTaskService.Add(
                10,
                A<OfflineTask.OfflineTaskAction>.That.IsEqualTo(OfflineTask.OfflineTaskAction.UnmarkAsRead),
                A<List<Tag>>.Ignored,
                A<List<Tag>>.Ignored)).MustHaveHappened();
            Assert.True(propertyChangedEventWasFired);
            Assert.False(database.Get<Item>(10).IsRead);
        }

        [Fact]
        public void MarkingAnItemAsStarredInvokesProperActions()
        {
            var item = A.Fake<Item>(x => x.WithArgumentsForConstructor(() => new Item()
            {
                Id = 10,
                IsStarred = false
            }));
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var navigation = A.Fake<INavigationService>();
            var logging = A.Fake<ILoggingService>();
            var device = A.Fake<IPlatformSpecific>();
            var database = TestsHelper.CreateFakeDatabase();

            database.Insert(item);

            var viewModel = new ItemViewModel(item,
                offlineTaskService,
                navigation,
                logging,
                device,
                database);

            bool propertyChangedEventWasFired = false;
            viewModel.PropertyChanged += (s, e) => propertyChangedEventWasFired = true;

            viewModel.MarkAsStarredCommand.Execute(null);

            A.CallTo(() => offlineTaskService.Add(
                10,
                A<OfflineTask.OfflineTaskAction>.That.IsEqualTo(OfflineTask.OfflineTaskAction.MarkAsStarred),
                A<List<Tag>>.Ignored,
                A<List<Tag>>.Ignored)).MustHaveHappened();
            Assert.True(propertyChangedEventWasFired);
            Assert.True(database.Get<Item>(10).IsStarred);
        }

        [Fact]
        public void MarkingAnItemAsUnstarredInvokesProperActions()
        {
            var item = A.Fake<Item>(x => x.WithArgumentsForConstructor(() => new Item()
            {
                Id = 10,
                IsStarred = true
            }));
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var navigation = A.Fake<INavigationService>();
            var logging = A.Fake<ILoggingService>();
            var device = A.Fake<IPlatformSpecific>();
            var database = TestsHelper.CreateFakeDatabase();

            database.Insert(item);

            var viewModel = new ItemViewModel(item,
                offlineTaskService,
                navigation,
                logging,
                device,
                database);

            bool propertyChangedEventWasFired = false;
            viewModel.PropertyChanged += (s, e) => propertyChangedEventWasFired = true;

            viewModel.UnmarkAsStarredCommand.Execute(null);

            A.CallTo(() => offlineTaskService.Add(
                10,
                A<OfflineTask.OfflineTaskAction>.That.IsEqualTo(OfflineTask.OfflineTaskAction.UnmarkAsStarred),
                A<List<Tag>>.Ignored,
                A<List<Tag>>.Ignored)).MustHaveHappened();
            Assert.True(propertyChangedEventWasFired);
            Assert.False(database.Get<Item>(10).IsStarred);
        }

        [Fact]
        public void DeletingAnItemInvokesProperActions()
        {
            var item = A.Fake<Item>(x => x.WithArgumentsForConstructor(() => new Item()
            {
                Id = 10
            }));
            var offlineTaskService = A.Fake<IOfflineTaskService>();
            var navigation = A.Fake<INavigationService>();
            var logging = A.Fake<ILoggingService>();
            var device = A.Fake<IPlatformSpecific>();
            var database = TestsHelper.CreateFakeDatabase();

            database.Insert(item);

            var viewModel = new ItemViewModel(item,
                offlineTaskService,
                navigation,
                logging,
                device,
                database);
            
            viewModel.DeleteCommand.Execute(null);

            A.CallTo(() => offlineTaskService.Add(
                10,
                A<OfflineTask.OfflineTaskAction>.That.IsEqualTo(OfflineTask.OfflineTaskAction.Delete),
                A<List<Tag>>.Ignored,
                A<List<Tag>>.Ignored)).MustHaveHappened();
            Assert.Null(database.Find<Item>(10));
        }
    }
}
