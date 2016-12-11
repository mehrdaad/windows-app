using SQLite.Net.Attributes;
using System.Collections.Generic;

namespace wallabag.Models
{
    public class OfflineTask
    {
        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        public int ItemId { get; set; }
        public OfflineTaskAction Action { get; set; }
        public List<Tag> AddedTags { get; set; }
        public List<Tag> RemovedTags { get; set; }

        public string Url { get; set; }
        public List<string> Tags { get; set; }

        public OfflineTask() { }

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
        
        public void Invert()
        {
            switch (Action)
            {
                case OfflineTaskAction.MarkAsRead:
                    Action = OfflineTaskAction.UnmarkAsRead;
                    break;
                case OfflineTaskAction.UnmarkAsRead:
                    Action = OfflineTaskAction.UnmarkAsRead;
                    break;
                case OfflineTaskAction.MarkAsStarred:
                    Action = OfflineTaskAction.UnmarkAsStarred;
                    break;
                case OfflineTaskAction.UnmarkAsStarred:
                    Action = OfflineTaskAction.MarkAsRead;
                    break;
                case OfflineTaskAction.AddItem:
                    Action = OfflineTaskAction.Delete;
                    break;
                case OfflineTaskAction.Delete:
                    Action = OfflineTaskAction.AddItem;
                    break;
                default:
                    break;
            }
        }
    }
}
