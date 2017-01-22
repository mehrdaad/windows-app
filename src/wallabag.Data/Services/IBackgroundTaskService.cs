using System.Threading.Tasks;

namespace wallabag.Data.Services
{
    public interface IBackgroundTaskService
    {
        Task RegisterBackgroundTaskAsync();
        void UnregisterBackgroundTask();

        bool IsRegistered { get; }
        bool IsSupported { get; }
    }
}
