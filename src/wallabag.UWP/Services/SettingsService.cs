using System.Collections.Generic;
using wallabag.Data.Services;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace wallabag.Services
{
    class SettingsService : ISettingsService
    {
        public T GetValueOrDefault<T>(string key, T defaultValue = default(T), SettingStrategy strategy = SettingStrategy.Local, string containerName = "")
        {
            var container = GetContainerValuesForStrategyAndContainerName(strategy, containerName);
            if (container.ContainsKey(key))
                return (T)container[key];

            return defaultValue;
        }

        public void AddOrUpdateValue<T>(string key, T value, SettingStrategy strategy = SettingStrategy.Local, string containerName = "")
        {
            var container = GetContainerValuesForStrategyAndContainerName(strategy, containerName);

            container[key] = value;
        }

        public void Remove(string key, SettingStrategy strategy = SettingStrategy.Roaming, string containerName = "")
            => GetContainerValuesForStrategyAndContainerName(strategy, containerName).Remove(key);

        public void Clear(SettingStrategy strategy = SettingStrategy.Roaming, string containerName = "")
            => GetContainerValuesForStrategyAndContainerName(strategy, containerName).Clear();

        public bool Contains(string key, SettingStrategy strategy = SettingStrategy.Local, string containerName = "")
            => GetContainerValuesForStrategyAndContainerName(strategy, containerName).ContainsKey(key);

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

        private IPropertySet GetContainerValuesForStrategyAndContainerName(SettingStrategy strategy, string containerName)
        {
            var container = ApplicationData.Current.LocalSettings;

            if (strategy == SettingStrategy.Roaming)
                container = ApplicationData.Current.RoamingSettings;

            if (!string.IsNullOrWhiteSpace(containerName))
                container = container.CreateContainer(containerName, ApplicationDataCreateDisposition.Always);

            return container.Values;
        }
    }
}
