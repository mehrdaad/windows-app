using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using wallabag.Api.Models;
using wallabag.Common;
using wallabag.Common.Helpers;
using wallabag.Common.Messages;
using wallabag.Models;
using static wallabag.Models.OfflineTask;

namespace wallabag.Services
{
    class OfflineTaskService
    {
        private static Dictionary<int, OfflineTask> _tasks = new Dictionary<int, OfflineTask>();

        private static Delayer _delayer = CreateDelayer();
        private static Delayer CreateDelayer()
        {
            var d = new Delayer(SettingsService.Instance.UndoTimeout);
            d.Action += async (s, e) =>
            {
                foreach (var item in _tasks)
                {
                    if (await ExecuteAsync(item.Value))
                        _tasks.Remove(item.Key);
                }
            };
            return d;
        }

        public static Task<bool> ExecuteAsync(OfflineTask task) => ExecuteAsync(task.Id);
        public static async Task<bool> ExecuteAsync(int taskId)
        {
            var task = _tasks[taskId];

            if (GeneralHelper.InternetConnectionIsAvailable == false)
                return false;

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
                    var item = App.Database.Get<Item>(i => i.Id == task.ItemId);

                    if (task.AddedTags?.Count > 0)
                    {
                        var newTags = await App.Client.AddTagsAsync(task.ItemId, task.AddedTags.ToStringArray());

                        if (newTags != null)
                        {
                            var convertedTags = new ObservableCollection<Tag>();
                            foreach (var tag in newTags)
                                convertedTags.Add(tag);

                            item.Tags.Replace(convertedTags);
                        }
                    }
                    if (task.RemovedTags?.Count > 0)
                    {
                        List<WallabagTag> tagsToRemove = new List<WallabagTag>();
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
                App.Database.Delete(task);
                App.OfflineTaskRemoved?.Invoke(task, task);
            }

            return executionIsSuccessful;
        }
        public static void AddTask(string url, IEnumerable<string> newTags = null, string title = "", bool invokeAddedEvent = true)
        {
            var newTask = new OfflineTask();

            newTask.ItemId = GeneralHelper.LastItemId;
            newTask.Action = OfflineTaskAction.AddItem;
            newTask.Url = url;
            newTask.Tags = newTags.ToList();

            App.Database.Insert(newTask);

            if (invokeAddedEvent)
                App.OfflineTaskAdded?.Invoke(null, newTask);
        }
        public static void AddTask(int itemId, OfflineTaskAction action, List<Tag> addTagsList = null, List<Tag> removeTagsList = null)
        {
            var newTask = new OfflineTask();

            newTask.ItemId = itemId;
            newTask.Action = action;
            newTask.AddedTags = addTagsList;
            newTask.RemovedTags = removeTagsList;

            App.Database.Insert(newTask);
            App.OfflineTaskAdded?.Invoke(null, newTask);
        }
    }
}
