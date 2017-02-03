using System.Collections.Generic;
using wallabag.Data.Services;
using Windows.Storage;

namespace wallabag.Services
{
    class SettingsService : ISettingsService
    {
        public T GetValueOrDefault<T>(string key, T defaultValue = default(T), SettingStrategy strategy = SettingStrategy.Local, string containerName = "")
        {
            var container = GetContainerForStrategyAndName(strategy, containerName);
            if (container.Values.ContainsKey(key))
                return (T)container.Values[key];

            return defaultValue;
        }

        public void AddOrUpdateValue<T>(string key, T value, SettingStrategy strategy = SettingStrategy.Local, string containerName = "")
        {
            var container = GetContainerForStrategyAndName(strategy, containerName);

            container.Values[key] = value;
        }

        public void Remove(string key, SettingStrategy strategy = SettingStrategy.Roaming, string containerName = "")
            => GetContainerForStrategyAndName(strategy, containerName).Values.Remove(key);

        public void Clear(SettingStrategy strategy = SettingStrategy.Roaming, string containerName = "")
            => GetContainerForStrategyAndName(strategy, containerName).Values.Clear();

        public bool Contains(string key, SettingStrategy strategy = SettingStrategy.Local, string containerName = "")
            => GetContainerForStrategyAndName(strategy, containerName).Values.ContainsKey(key);

        public void ClearAll()
        {
            var containersToClear = new List<ApplicationDataContainer>
            {
                ApplicationData.Current.LocalSettings,
                ApplicationData.Current.RoamingSettings
            };

            foreach (var container in containersToClear)
            {
                container.Values.Clear();

                foreach (var item in container.Containers)
                    container.DeleteContainer(item.Key);
            }
        }

        private ApplicationDataContainer GetContainerForStrategyAndName(SettingStrategy strategy, string containerName)
        {
            var container = ApplicationData.Current.LocalSettings;

            if (strategy == SettingStrategy.Roaming)
                container = ApplicationData.Current.RoamingSettings;

            if (!string.IsNullOrWhiteSpace(containerName))
                container = container.CreateContainer(containerName, ApplicationDataCreateDisposition.Always);

            return container;
        }
    }
}
