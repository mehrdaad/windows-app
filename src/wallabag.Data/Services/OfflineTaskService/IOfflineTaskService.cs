using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Data.Models;

namespace wallabag.Data.Services.OfflineTaskService
{
    public interface IOfflineTaskService
    {
        int Count { get; }

        Task ExecuteAllAsync();
        void Add(string url, IEnumerable<string> newTags);
        void Add(int itemId, OfflineTask.OfflineTaskAction action, List<Tag> addedTags = null, List<Tag> removedTags = null);

        event EventHandler<OfflineTask> TaskAdded;
        event EventHandler<OfflineTaskExecutedEventArgs> TaskExecuted;
    }
}
