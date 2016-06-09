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
        public List<Tag> newTagsList { get; set; }

        public Task ExecuteAsync()
        {
            return Task.CompletedTask;
        }

        public OfflineTask(int itemId, OfflineTaskAction action, IEnumerable<Tag> newTags = null)
        {
            ItemId = itemId;
            Action = action;
            newTagsList = newTags as List<Tag>;
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
