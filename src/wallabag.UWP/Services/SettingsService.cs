using GalaSoft.MvvmLight.Ioc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using wallabag.Data.Services;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace wallabag.Services
{
    class SettingsService : ISettingsService
    {
        private ILoggingService _loggingService => SimpleIoc.Default.GetInstance<ILoggingService>();

        public T GetValueOrDefault<T>(string key, T defaultValue = default(T), SettingStrategy strategy = SettingStrategy.Local, string containerName = "")
        {
            _loggingService.WriteLine($"Trying to fetch setting {key} from {strategy.ToString().ToLower()} container {containerName}.");

            try
            {
                var values = GetContainerValuesForStrategyAndContainerName(strategy, containerName);

                if (values.ContainsKey(key))
                {
                    var container = values[key] as ApplicationDataCompositeValue;

                    var type = typeof(T);
                    if (container.ContainsKey("Type"))
                        type = Type.GetType((string)container["Type"]);

                    string value = null;
                    if (container.ContainsKey("Value"))
                        value = container["Value"] as string;

                    var converted = (T)JsonConvert.DeserializeObject(value, type);

                    _loggingService.WriteLine($"Returning value: {converted}");

                    return converted;
                }

                _loggingService.WriteLine($"Returning default value: {defaultValue}");
                return defaultValue;
            }
            catch
            {
                _loggingService.WriteLine($"Returning default value: {defaultValue}");
                return defaultValue;
            }
        }

        public void AddOrUpdateValue<T>(string key, T value, SettingStrategy strategy = SettingStrategy.Local, string containerName = "")
        {
            _loggingService.WriteLine($"Adding setting {key} to {strategy.ToString().ToLower()} container {containerName} with value '{value}'.");

            var type = typeof(T);

            if (value != null)
                type = value.GetType();

            var container = new ApplicationDataCompositeValue();
            var converted = JsonConvert.SerializeObject(value, Formatting.None);

            if (converted != null)
                container["Value"] = converted;

            if ((type != typeof(string) && !type.GetTypeInfo().IsValueType) || (type != typeof(T)))
                container["Type"] = type.AssemblyQualifiedName;

            GetContainerValuesForStrategyAndContainerName(strategy, containerName)[key] = container;
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
