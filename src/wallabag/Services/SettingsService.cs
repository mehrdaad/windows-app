using System;
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
            else if (typeof(T) == typeof(DateTime))
                _container.Values.Add(key, value.ToString());
            else
                _container.Values.Add(key, value);

            return true;
        }
        public T GetValueOrDefault<T>(string key, T defaultValue = default(T))
        {
            if (Contains(key))
            {
                if (typeof(T) == typeof(DateTime))
                    return (T)(object)DateTime.Parse(_container.Values[key] as string);
                else
                    return (T)_container.Values[key];
            }
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
