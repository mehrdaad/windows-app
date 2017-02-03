using System;
using System.Collections.Generic;
using wallabag.Data.Services;
using Windows.Storage;

namespace wallabag.Services
{
    // TODO: Add serialization for non supported value types
    class SettingsService : Data.Services.ISettingsService
    {
        private List<Type> _supportedTypes = new List<Type>()
        {
            typeof(bool),
            typeof(UInt16),
            typeof(Int16),
            typeof(Int32),
            typeof(UInt32),
            typeof(UInt64),
            typeof(Single),
            typeof(double),
            typeof(char),
            typeof(string),
            typeof(DateTime),
            typeof(TimeSpan),
            typeof(Guid),
            typeof(ApplicationDataCompositeValue)
        };
        private ApplicationDataContainer _localContainer => ApplicationData.Current.LocalSettings;

        public T GetValueOrDefault<T>(string key, T defaultValue = default(T), SettingStrategy strategy = SettingStrategy.Local, string container = "")
        {
            throw new NotImplementedException();
        }

        public bool AddOrUpdateValue<T>(string key, T value, SettingStrategy strategy = SettingStrategy.Local, string container = "")
        {
            throw new NotImplementedException();
        }

        public void Remove(string key, SettingStrategy strategy = SettingStrategy.Roaming, string container = "")
        {
            throw new NotImplementedException();
        }

        public void Clear(SettingStrategy strategy = SettingStrategy.Roaming, string container = "")
        {
            throw new NotImplementedException();
        }

        public bool Contains(string key, SettingStrategy strategy = SettingStrategy.Local, string container = "")
        {
            throw new NotImplementedException();
        }
    }
}
