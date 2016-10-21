using GalaSoft.MvvmLight.Messaging;
using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using wallabag.Api.Models;
using wallabag.Common.Helpers;
using wallabag.Common.Messages;

namespace wallabag.Models
{
    public class OfflineTask
    {
        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        public int ItemId { get; set; }
        public OfflineTaskAction Action { get; set; }
        public List<Tag> addTagsList { get; set; }
        public List<Tag> removeTagsList { get; set; }

        public string Url { get; set; }
        public List<string> Tags { get; set; }

        public OfflineTask() { }

        public async Task ExecuteAsync()
        {
            if (GeneralHelper.InternetConnectionIsAvailable == false)
                return;

            bool executionIsSuccessful = false;
            switch (Action)
            {
                case OfflineTaskAction.MarkAsRead:
                    executionIsSuccessful = await App.Client.ArchiveAsync(ItemId);
                    break;
                case OfflineTaskAction.UnmarkAsRead:
                    executionIsSuccessful = await App.Client.UnarchiveAsync(ItemId);
                    break;
                case OfflineTaskAction.MarkAsStarred:
                    executionIsSuccessful = await App.Client.FavoriteAsync(ItemId);
                    break;
                case OfflineTaskAction.UnmarkAsStarred:
                    executionIsSuccessful = await App.Client.UnfavoriteAsync(ItemId);
                    break;
                case OfflineTaskAction.EditTags:
                    var item = App.Database.Get<Item>(i => i.Id == ItemId);

                    if (addTagsList?.Count > 0)
                    {
                        var newTags = await App.Client.AddTagsAsync(ItemId, addTagsList.ToStringArray());

                        if (newTags != null)
                        {
                            var convertedTags = new ObservableCollection<Tag>();
                            foreach (var tag in newTags)
                                convertedTags.Add(tag);

                            item.Tags.Replace(convertedTags);
                        }
                    }
                    if (removeTagsList?.Count > 0)
                    {
                        List<WallabagTag> tagsToRemove = new List<WallabagTag>();
                        foreach (var tag in removeTagsList)
                            tagsToRemove.Add(tag);

                        if (await App.Client.RemoveTagsAsync(ItemId, tagsToRemove))
                            foreach (var tag in removeTagsList)
                                if (item.Tags.Contains(tag))
                                    item.Tags.Remove(tag);
                    }

                    executionIsSuccessful = App.Database.Update(item) == 1;
                    break;
                case OfflineTaskAction.AddItem:
                    var newItem = await App.Client.AddAsync(new Uri(Url), Tags);

                    if (newItem != null)
                    {
                        App.Database.InsertOrReplace((Item)newItem);
                        Messenger.Default.Send(new UpdateItemMessage(newItem.Id));
                    }

                    executionIsSuccessful = newItem != null;
                    break;
                case OfflineTaskAction.Delete:
                    executionIsSuccessful = await App.Client.DeleteAsync(ItemId);
                    break;
                default:
                    break;
            }

            if (executionIsSuccessful)
            {
                App.Database.Delete(this);
                App.OfflineTaskRemoved?.Invoke(this, this);
            }
        }
        public static void Add(string url, IEnumerable<string> newTags, string title = "", bool invokeAddedEvent = true)
        {
            var newTask = new OfflineTask();

            newTask.ItemId = LastItemId;
            newTask.Action = OfflineTaskAction.AddItem;
            newTask.Url = url;
            newTask.Tags = newTags.ToList();

            App.Database.Insert(newTask);

            if (invokeAddedEvent)
                App.OfflineTaskAdded?.Invoke(null, newTask);
        }
        public static void Add(int itemId, OfflineTaskAction action, List<Tag> addTagsList = null, List<Tag> removeTagsList = null)
        {
            var newTask = new OfflineTask();

            newTask.ItemId = itemId;
            newTask.Action = action;
            newTask.addTagsList = addTagsList;
            newTask.removeTagsList = removeTagsList;

            App.Database.Insert(newTask);
            App.OfflineTaskAdded?.Invoke(null, newTask);
        }

        internal static int LastItemId => App.Database.ExecuteScalar<int>("select Max(ID) from 'Item'", new object[0]);

        public enum OfflineTaskAction
        {
            MarkAsRead = 0,
            UnmarkAsRead = 1,
            MarkAsStarred = 2,
            UnmarkAsStarred = 3,
            EditTags = 4,
            AddItem = 5,
            Delete = 10
        }
    }
}
