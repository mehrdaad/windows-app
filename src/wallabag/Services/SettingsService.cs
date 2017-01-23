using wallabag.Data.Services;
using Windows.Storage;

namespace wallabag.Services
{
    class SettingsService : ISettingsService
    {
        private ApplicationDataContainer _container;

        public SettingsService()
        {
            _container = ApplicationData.Current.RoamingSettings;
        }

        public bool AddOrUpdateValue<T>(string key, T value)
        {
            if (Contains(key))
                _container.Values[key] = value;
            else
                _container.Values.Add(key, value);

            return true;
        }
        public T GetValueOrDefault<T>(string key, T defaultValue = default(T))
        {
            if (Contains(key))
                return (T)_container.Values[key];
            else
                return defaultValue;
        }

        public void Clear() => _container.Values.Clear();
        public bool Contains(string key) => _container.Values.ContainsKey(key);
        public void Remove(string key)
        {
            if (Contains(key))
                _container.Values.Remove(key);
        }
    }
}
