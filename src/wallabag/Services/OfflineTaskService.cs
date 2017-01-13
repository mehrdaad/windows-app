using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using wallabag.Api.Models;
using wallabag.Common.Helpers;
using wallabag.Common.Messages;
using wallabag.Models;
using static wallabag.Models.OfflineTask;

namespace wallabag.Services
{
    class OfflineTaskService
    {
        private static ObservableCollection<OfflineTask> _tasks;
        public static ObservableCollection<OfflineTask> Tasks
        {
            get
            {
                if (_tasks == null)
                {
                    _tasks = new ObservableCollection<OfflineTask>(App.Database.Table<OfflineTask>());
                    _tasks.CollectionChanged += async (s, e) =>
                     {
                         if (e.NewItems != null && e.NewItems.Count > 0)
                             await ExecuteAsync(e.NewItems[0] as OfflineTask);
                     };
                }

                return _tasks;
            }
        }

        internal static async Task ExecuteAllAsync()
        {
            foreach (var task in _tasks)
                await ExecuteAsync(task);
        }
        private static async Task ExecuteAsync(OfflineTask task)
        {
            if (GeneralHelper.InternetConnectionIsAvailable == false)
                return;

            bool executionIsSuccessful = false;
            switch (task.Action)
            {
                case OfflineTaskAction.MarkAsRead:
                    executionIsSuccessful = await App.Client.ArchiveAsync(task.ItemId);
                    break;
                case OfflineTaskAction.UnmarkAsRead:
                    executionIsSuccessful = await App.Client.UnarchiveAsync(task.ItemId);
                    break;
                case OfflineTaskAction.MarkAsStarred:
                    executionIsSuccessful = await App.Client.FavoriteAsync(task.ItemId);
                    break;
                case OfflineTaskAction.UnmarkAsStarred:
                    executionIsSuccessful = await App.Client.UnfavoriteAsync(task.ItemId);
                    break;
                case OfflineTaskAction.EditTags:
                    var item = App.Database.Find<Item>(i => i.Id == task.ItemId);

                    if (item == null)
                    {
                        /* This can happen in several cases even if it shouldn't. 
                         * In the case the item is already deleted, the task will be marked as success so it's removed asap. */
                        executionIsSuccessful = true;
                        break;
                    }

                    if (task.AddedTags?.Count > 0)
                    {
                        var newTags = await App.Client.AddTagsAsync(task.ItemId, task.AddedTags.ToStringArray());

                        if (newTags != null)
                        {
                            var convertedTags = new ObservableCollection<Tag>();
                            foreach (var tag in newTags)
                                convertedTags.Add(tag);

                            App.Database.InsertOrReplaceAll(convertedTags);
                            item.Tags.Replace(convertedTags);
                        }
                    }
                    if (task.RemovedTags?.Count > 0)
                    {
                        var tagsToRemove = new List<WallabagTag>();
                        foreach (var tag in task.RemovedTags)
                            tagsToRemove.Add(tag);

                        if (await App.Client.RemoveTagsAsync(task.ItemId, tagsToRemove))
                            foreach (var tag in task.RemovedTags)
                                if (item.Tags.Contains(tag))
                                    item.Tags.Remove(tag);
                    }

                    executionIsSuccessful = App.Database.Update(item) == 1;
                    break;
                case OfflineTaskAction.AddItem:
                    var newItem = await App.Client.AddAsync(new Uri(task.Url), task.Tags);

                    if (newItem != null)
                    {
                        App.Database.InsertOrReplace((Item)newItem);
                        Messenger.Default.Send(new UpdateItemMessage(newItem.Id));
                    }

                    executionIsSuccessful = newItem != null;
                    break;
                case OfflineTaskAction.Delete:
                    executionIsSuccessful = await App.Client.DeleteAsync(task.ItemId);
                    break;
                default:
                    break;
            }

            if (executionIsSuccessful)
            {
                Tasks.Remove(task);
                App.Database.Delete(task);
            }
        }

        public static void Add(string url, IEnumerable<string> newTags, string title = "")
        {
            var newTask = new OfflineTask()
            {
                ItemId = LastItemId,
                Action = OfflineTaskAction.AddItem,
                Url = url,
                Tags = newTags.ToList()
            };
            InsertTask(newTask);
        }
        public static void Add(int itemId, OfflineTaskAction action, List<Tag> addTagsList = null, List<Tag> removeTagsList = null)
        {
            var newTask = new OfflineTask()
            {
                ItemId = itemId,
                Action = action,
                AddedTags = addTagsList,
                RemovedTags = removeTagsList
            };
            InsertTask(newTask);
        }
        private static void InsertTask(OfflineTask newTask)
        {
            Tasks.Add(newTask);
            App.Database.Insert(newTask);
        }

        internal static int LastItemId => App.Database.ExecuteScalar<int>("select Max(ID) from 'Item'");
    }
}
