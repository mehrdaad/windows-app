using SQLite.Net.Attributes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace wallabag.Models
{
    public class OfflineTask
    {
        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        public int ItemId { get; set; }
        public OfflineTaskAction Action { get; set; }
        public IEnumerable<Tag> addTagsList { get; set; }
        public IEnumerable<Tag> removeTagsList { get; set; }

        public Task ExecuteAsync()
        {
            return Task.CompletedTask;
        }

        public OfflineTask(int itemId, OfflineTaskAction action, IEnumerable<Tag> addTagsEnumerable = null, IEnumerable<Tag> removeTagsEnumerable = null)
        {
            ItemId = itemId;
            Action = action;
            addTagsList = addTagsEnumerable;
            removeTagsList = removeTagsEnumerable;
        }

        public enum OfflineTaskAction
        {
            MarkAsRead = 0,
            UnmarkAsRead = 1,
            MarkAsStarred = 2,
            UnmarkAsStarred = 3,
            EditTags = 4,
            Delete = 10
        }
    }
}
