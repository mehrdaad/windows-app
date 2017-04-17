using SQLite.Net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using wallabag.Api;
using wallabag.Api.Models;
using wallabag.Data.Common.Helpers;
using wallabag.Data.Interfaces;
using wallabag.Data.Models;
using static wallabag.Data.Models.OfflineTask;

namespace wallabag.Data.Services.OfflineTaskService
{
    public class OfflineTaskService : IOfflineTaskService
    {
        private readonly IWallabagClient _client;
        private readonly SQLiteConnection _database;
        private readonly ILoggingService _loggingService;
        private readonly IPlatformSpecific _platform;
        private int _lastItemId => _database.ExecuteScalar<int>("select Max(ID) from 'Item'");

        public const string m_PLACEHOLDER_PREFIX = "//wallabag-placeholder-";
        public int Count => _database.ExecuteScalar<int>("select count(*) from OfflineTask");
        public event EventHandler<OfflineTaskAddedEventArgs> TaskAdded;
        public event EventHandler<OfflineTaskExecutedEventArgs> TaskExecuted;

        public OfflineTaskService(IWallabagClient client, SQLiteConnection database, ILoggingService loggingService, IPlatformSpecific platform)
        {
            _client = client;
            _database = database;
            _loggingService = loggingService;
            _platform = platform;

            this.TaskAdded += async (s, e) => await ExecuteAsync(e.Task);
        }

        public async Task ExecuteAllAsync()
        {
            _loggingService.WriteLine($"Executing all offline tasks. Number of tasks: {Count}");

            var tasks = _database.Table<OfflineTask>();
            foreach (var task in tasks)
                await ExecuteAsync(task);

            _loggingService.WriteLine($"Execution finished. Number of failed tasks: {Count}");
        }
        private async Task<bool> ExecuteAsync(OfflineTask task)
        {
            _loggingService.WriteLine($"Executing task {task.Id} with action {task.Action} for item {task.ItemId}.");
            int placeholderId = -1;

            if (_platform.InternetConnectionIsAvailable == false)
            {
                _loggingService.WriteLine("No internet connection available. Cancelled.");
                return false;
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
                    placeholderId = _database.FindWithQuery<Item>("select Id from Item where Content=?", m_PLACEHOLDER_PREFIX + task.Id)?.Id ?? -1;

                    if (placeholderId >= 0)
                        _database.Delete<OfflineTask>(placeholderId);

                    var newItem = await _client.AddAsync(new Uri(task.Url), task.Tags);
                    if (newItem != null)
                        _database.InsertOrReplace((Item)newItem);

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
                _loggingService.WriteLine($"Execution of task {task.Id} was successful.");
                _database.Delete(task);
            }
            _loggingService.WriteLineIf(!executionIsSuccessful, "Execution was not successful.", LoggingCategory.Warning);

            TaskExecuted?.Invoke(this, new OfflineTaskExecutedEventArgs(task, placeholderId, executionIsSuccessful));

            return executionIsSuccessful;
        }

        public Task AddAsync(string url, IEnumerable<string> newTags)
        {
            _loggingService.WriteLine($"Adding task for URL '{url}' with {newTags.Count()} tags: {string.Join(",", newTags)}");
            Uri.TryCreate(url, UriKind.Absolute, out var uri);

            var newTask = new OfflineTask()
            {
                ItemId = _lastItemId,
                Action = OfflineTaskAction.AddItem,
                Url = url,
                Tags = newTags.ToList()
            };

        

            _database.Insert(newTask);

            // Fetch task ID from database
            newTask.Id = _database.FindWithQuery<OfflineTask>("select Id from OfflineTask where ItemId=? and Action=?", newTask.ItemId, newTask.Action).Id;

            _loggingService.WriteLine($"Inserting new placeholder item for task {newTask.Id} into the database.");
            _database.Insert(new Item()
            {
                Id = _lastItemId + 1,
                Title = uri.Host,
                Url = url,
                Hostname = uri.Host,
                Content = m_PLACEHOLDER_PREFIX + newTask.Id
            });

            _tasks.Add(newTask);

            int placeholderItemId = _database.FindWithQuery<Item>("select Id from Item where Content=?", m_PLACEHOLDER_PREFIX + newTask.Id).Id;
            TaskAdded?.Invoke(this, new OfflineTaskAddedEventArgs(newTask, placeholderItemId));

            return Task.FromResult(true);
        }
        public Task AddAsync(int itemId, OfflineTaskAction action, List<Tag> addTagsList = null, List<Tag> removeTagsList = null)
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

            return Task.FromResult(true);
        }
        private void InsertTask(OfflineTask newTask)
        {
            _loggingService.WriteLine("Inserting task into database.");
            _database.Insert(newTask);

            TaskAdded?.Invoke(this, new OfflineTaskAddedEventArgs(newTask));
        }
    }
}
