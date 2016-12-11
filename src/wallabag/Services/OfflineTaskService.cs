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
        private static Delayer _delayer;

        public static bool IsBlocked { get; set; } = false;

        public static EventHandler<OfflineTask> OfflineTaskAdded;
        public static EventHandler<OfflineTask> OfflineTaskRemoved;

        public static void Initialize()
        {
            if (_delayer == null)
            {
                _delayer = new Delayer(SettingsService.Instance.UndoTimeout);
                _delayer.Action += async (s, e) => await ExecuteAllAsync();
            }

            // Fetch tasks from the database, so the dictionary contains all relevant tasks.        
            var databaseTasks = App.Database.Table<OfflineTask>().ToList();
            foreach (var item in databaseTasks)
                _tasks.Add(item.Id, item);
        }

        public static async Task<bool> ExecuteAsync(OfflineTask task)
        {
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
                RemoveTask(task);

            return executionIsSuccessful;
        }
        public static async Task ExecuteAllAsync()
        {
            foreach (var item in _tasks)
            {
                if (await ExecuteAsync(item.Value))
                    RemoveTask(item.Value);
            }

            if (Count() == 0)
                _delayer.Stop();
        }

        public static void Add(string url, IEnumerable<string> newTags = null, string title = "")
        {
            var newTask = new OfflineTask();

            newTask.ItemId = GeneralHelper.LastItemId;
            newTask.Action = OfflineTaskAction.AddItem;
            newTask.Url = url;
            newTask.Tags = newTags.ToList();

            AddTask(newTask);
        }
        public static void Add(int itemId, OfflineTaskAction action, List<Tag> addTagsList = null, List<Tag> removeTagsList = null)
        {
            var newTask = new OfflineTask();

            newTask.ItemId = itemId;
            newTask.Action = action;
            newTask.AddedTags = addTagsList;
            newTask.RemovedTags = removeTagsList;

            _delayer.ResetAndTick();

            AddTask(newTask);
        }

        public static bool Remove(OfflineTask task)
        {
            if (_tasks.ContainsKey(task.Id))
            {
                RemoveTask(task);
                return true;
            }
            else return false;
        }

        private static void AddTask(OfflineTask task)
        {
            if (task.Id == 0)
                task.Id = LastTaskId;

            if (task.Id == 0)
                task.Id = _tasks.Count + 1;

            _tasks.Add(task.Id, task);
            App.Database.Insert(task);
            OfflineTaskAdded?.Invoke(null, task);
        }
        private static void RemoveTask(OfflineTask task)
        {
            _tasks.Remove(task.Id);
            App.Database.Delete(task);
            OfflineTaskRemoved?.Invoke(null, task);
        }

        private static int LastTaskId => App.Database.ExecuteScalar<int>("select max(ID) from OfflineTask");
        public static int Count() => App.Database.ExecuteScalar<int>("select count(*) from OfflineTask");
    }
}
