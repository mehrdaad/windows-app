using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using SQLite.Net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using wallabag.Api;
using wallabag.Api.Models;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Common.Messages;
using wallabag.Data.Models;
using static wallabag.Data.Models.OfflineTask;

namespace wallabag.Data.Services
{
    public class OfflineTaskService
    {
        private static IWallabagClient _client => SimpleIoc.Default.GetInstance<IWallabagClient>();
        private static SQLiteConnection _database => SimpleIoc.Default.GetInstance<SQLiteConnection>();
        private static ILoggingService _loggingService => SimpleIoc.Default.GetInstance<ILoggingService>();

        private static ObservableCollection<OfflineTask> _tasks;
        public static ObservableCollection<OfflineTask> Tasks
        {
            get
            {
                if (_tasks == null)
                {
                    _tasks = new ObservableCollection<OfflineTask>(_database.Table<OfflineTask>());
                    _tasks.CollectionChanged += async (s, e) =>
                     {
                         if (e.NewItems != null && e.NewItems.Count > 0)
                             await ExecuteAsync(e.NewItems[0] as OfflineTask);
                     };
                }

                return _tasks;
            }
        }

        public static async Task ExecuteAllAsync()
        {
            _loggingService.WriteLine($"Executing all offline tasks. Number of tasks: {Tasks.Count}");

            foreach (var task in Tasks)
                await ExecuteAsync(task);

            _loggingService.WriteLine($"Execution finished. Number of failed tasks: {Tasks.Count}");
        }
        private static async Task ExecuteAsync(OfflineTask task)
        {
            _loggingService.WriteLine($"Executing task {task.Id} with action {task.Action} for item {task.ItemId}.");

            if (GeneralHelper.InternetConnectionIsAvailable == false)
            {
                _loggingService.WriteLine("No internet connection available. Cancelled.");
                return;
            }

            bool executionIsSuccessful = false;
            switch (task.Action)
            {
                case OfflineTaskAction.MarkAsRead:
                    executionIsSuccessful = await _client.ArchiveAsync(task.ItemId);
                    break;
                case OfflineTaskAction.UnmarkAsRead:
                    executionIsSuccessful = await _client.UnarchiveAsync(task.ItemId);
                    break;
                case OfflineTaskAction.MarkAsStarred:
                    executionIsSuccessful = await _client.FavoriteAsync(task.ItemId);
                    break;
                case OfflineTaskAction.UnmarkAsStarred:
                    executionIsSuccessful = await _client.UnfavoriteAsync(task.ItemId);
                    break;
                case OfflineTaskAction.EditTags:
                    var item = _database.Find<Item>(i => i.Id == task.ItemId);

                    if (item == null)
                    {
                        /* This can happen in several cases even if it shouldn't. 
                         * In the case the item is already deleted, the task will be marked as success so it's removed asap. */
                        executionIsSuccessful = true;
                        break;
                    }

                    if (task.AddedTags?.Count > 0)
                    {
                        var newTags = await _client.AddTagsAsync(task.ItemId, task.AddedTags.ToStringArray());

                        if (newTags != null)
                        {
                            var convertedTags = new ObservableCollection<Tag>();
                            foreach (var tag in newTags)
                                convertedTags.Add(tag);

                            _database.InsertOrReplaceAll(convertedTags);
                            item.Tags.Replace(convertedTags);
                        }
                    }
                    if (task.RemovedTags?.Count > 0)
                    {
                        var tagsToRemove = new List<WallabagTag>();
                        foreach (var tag in task.RemovedTags)
                            tagsToRemove.Add(tag);

                        if (await _client.RemoveTagsAsync(task.ItemId, tagsToRemove))
                            foreach (var tag in task.RemovedTags)
                                if (item.Tags.Contains(tag))
                                    item.Tags.Remove(tag);
                    }

                    executionIsSuccessful = _database.Update(item) == 1;
                    break;
                case OfflineTaskAction.AddItem:
                    var newItem = await _client.AddAsync(new Uri(task.Url), task.Tags);

                    if (newItem != null)
                    {
                        _database.InsertOrReplace((Item)newItem);
                        Messenger.Default.Send(new UpdateItemMessage(newItem.Id));
                    }

                    executionIsSuccessful = newItem != null;
                    break;
                case OfflineTaskAction.Delete:
                    executionIsSuccessful = await _client.DeleteAsync(task.ItemId);
                    break;
                default:
                    break;
            }

            if (executionIsSuccessful)
            {
                _loggingService.WriteLine("Execution was successful.");
                Tasks.Remove(task);
                _database.Delete(task);
            }

            _loggingService.WriteLineIf(!executionIsSuccessful, "Execution was not successful.", LoggingCategory.Warning);
        }

        public static void Add(string url, IEnumerable<string> newTags)
        {
            _loggingService.WriteLine($"Adding task for URL '{url}' with {newTags.Count()} tags: {string.Join(",", newTags)}");

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
            _loggingService.WriteLine($"Adding task for item {itemId} with action {action}. {addTagsList?.Count} new tags, {removeTagsList?.Count} removed tags.");

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
            _loggingService.WriteLine("Inserting task into database.");

            Tasks.Add(newTask);
            _database.Insert(newTask);
        }

        internal static int LastItemId => _database.ExecuteScalar<int>("select Max(ID) from 'Item'");
    }
}
