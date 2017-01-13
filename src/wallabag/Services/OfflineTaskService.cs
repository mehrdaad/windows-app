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
        public static async Task ExecuteAllAsync() { }
        private async Task<bool> ExecuteAsync(OfflineTask task)
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

                    if (task.addTagsList?.Count > 0)
                    {
                        var newTags = await App.Client.AddTagsAsync(task.ItemId, task.addTagsList.ToStringArray());

                        if (newTags != null)
                        {
                            var convertedTags = new ObservableCollection<Tag>();
                            foreach (var tag in newTags)
                                convertedTags.Add(tag);

                            App.Database.InsertOrReplaceAll(convertedTags);
                            item.Tags.Replace(convertedTags);
                        }
                    }
                    if (task.removeTagsList?.Count > 0)
                    {
                        List<WallabagTag> tagsToRemove = new List<WallabagTag>();
                        foreach (var tag in task.removeTagsList)
                            tagsToRemove.Add(tag);

                        if (await App.Client.RemoveTagsAsync(task.ItemId, tagsToRemove))
                            foreach (var tag in task.removeTagsList)
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

            return executionIsSuccessful;
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
                addTagsList = addTagsList,
                removeTagsList = removeTagsList
            };
            InsertTask(newTask);
        }
        private static void InsertTask(OfflineTask newTask) => App.Database.Insert(newTask);

        internal static int LastItemId => App.Database.ExecuteScalar<int>("select Max(ID) from 'Item'", new object[0]);
    }
}
