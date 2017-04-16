using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using wallabag.Data.Models;

namespace wallabag.Data.Services.OfflineTaskService
{
    public interface IOfflineTaskService
    {
        ObservableCollection<OfflineTask> Tasks { get; }
        int LastItemId { get; }

        Task ExecuteAllAsync();
        void Add(string url, IEnumerable<string> newTags);
        void Add(int itemId, OfflineTask.OfflineTaskAction action, List<Tag> addTagsList = null, List<Tag> removeTagsList = null);
    }
}
