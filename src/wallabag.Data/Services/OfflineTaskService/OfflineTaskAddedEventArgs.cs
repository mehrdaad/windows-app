using System;
using wallabag.Data.Models;

namespace wallabag.Data.Services.OfflineTaskService
{
    public class OfflineTaskAddedEventArgs : EventArgs
    {
        public OfflineTask Task { get; set; }
        public int PlaceholderItemId { get; set; } = -1;

        public OfflineTaskAddedEventArgs(OfflineTask task) => Task = task;
        public OfflineTaskAddedEventArgs(OfflineTask task, int placeholderItemId) : this(task) => PlaceholderItemId = placeholderItemId;
    }
}