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
        Task AddAsync(string url, IEnumerable<string> newTags);
        Task AddAsync(int itemId, OfflineTask.OfflineTaskAction action, List<Tag> addedTags = null, List<Tag> removedTags = null);

        event EventHandler<OfflineTaskAddedEventArgs> TaskAdded;
        event EventHandler<OfflineTaskExecutedEventArgs> TaskExecuted;
    }
}
