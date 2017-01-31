using System;
using System.Threading.Tasks;
using wallabag.Data.Common;
using wallabag.Data.Services;
using Windows.ApplicationModel.Background;

namespace wallabag.Services
{
    public class BackgroundTaskService : IBackgroundTaskService
    {
        private const string m_BACKGROUNDTASKNAME = "wallabagBackgroundSync";
        private IBackgroundTaskRegistration _backgroundTask;

        public async Task RegisterBackgroundTaskAsync()
        {
            if (IsSupported == false)
                return;

            await BackgroundExecutionManager.RequestAccessAsync();

            var builder = new BackgroundTaskBuilder()
            {
                Name = m_BACKGROUNDTASKNAME,
                IsNetworkRequested = true
            };
            builder.SetTrigger(new TimeTrigger((uint)Settings.BackgroundTask.ExecutionInterval.TotalMinutes, false));

            _backgroundTask = builder.Register();
        }
        public void UnregisterBackgroundTask()
        {
            if (IsRegistered)
                _backgroundTask.Unregister(false);
        }

        public bool IsRegistered
        {
            get
            {
                if (IsSupported == false)
                    return false;

                bool taskRegistered = false;
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == m_BACKGROUNDTASKNAME)
                    {
                        taskRegistered = true;
                        _backgroundTask = task.Value;
                        break;
                    }
                }
                return taskRegistered;
            }
        }
        public bool IsSupported => Windows.Foundation.Metadata.ApiInformation.IsMethodPresent(typeof(Windows.UI.Xaml.Application).FullName, "OnBackgroundActivated");
    }
}
