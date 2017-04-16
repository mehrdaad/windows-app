using System;
using wallabag.Data.Models;

namespace wallabag.Data.Services.OfflineTaskService
{
    public class OfflineTaskAddedEventArgs : EventArgs
    {
        public OfflineTask Task { get; set; }

        public OfflineTaskAddedEventArgs(OfflineTask task) => Task = task;
    }
}