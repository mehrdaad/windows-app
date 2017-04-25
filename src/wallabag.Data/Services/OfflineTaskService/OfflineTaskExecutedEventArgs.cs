using System;
using wallabag.Data.Models;

namespace wallabag.Data.Services.OfflineTaskService
{
    public class OfflineTaskExecutedEventArgs : EventArgs
    {
        public OfflineTask Task { get; set; }
        public int PlaceholderItemId { get; set; }
        public bool Success { get; set; }

        public OfflineTaskExecutedEventArgs(OfflineTask task)
            => Task = task;
        public OfflineTaskExecutedEventArgs(OfflineTask task, int placeholderItemId) : this(task)
            => PlaceholderItemId = placeholderItemId;
        public OfflineTaskExecutedEventArgs(OfflineTask task, int placeholderItemId, bool success) : this(task, placeholderItemId)
            => Success = success;
    }
}
