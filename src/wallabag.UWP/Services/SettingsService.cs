using Template10.Services.SettingsService;
using Windows.Storage;

namespace wallabag.Services
{
    class SettingsService : Data.Services.ISettingsService
    {
        ISettingsHelper _helper;

        // Set up the hook to the settings helper interface.
        public SettingsService()
        {
            _helper = new SettingsHelper();
        }

        public bool AddOrUpdateValue<T>(string key, T value)
        {
            _helper.Write(key, value);
            return true;
        }
        public T GetValueOrDefault<T>(string key, T defaultValue = default(T))
        {
            return _helper.Read(key, defaultValue, SettingsStrategies.Local);
        }

        public void Clear()
        {
            ApplicationData.Current.LocalSettings.Values.Clear();
            ApplicationData.Current.RoamingSettings.Values.Clear();

            foreach (var item in ApplicationData.Current.RoamingSettings.Containers)
                ApplicationData.Current.RoamingSettings.DeleteContainer(item.Key);

            foreach (var item in ApplicationData.Current.LocalSettings.Containers)
                ApplicationData.Current.LocalSettings.DeleteContainer(item.Key);
        }
        public bool Contains(string key) => _helper.Exists(key);
        public void Remove(string key) => _helper.Remove(key);
    }
}
