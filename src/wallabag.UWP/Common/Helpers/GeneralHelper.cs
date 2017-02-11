using System;
using Windows.System.Profile;
using WindowsStateTriggers;

namespace wallabag.Common.Helpers
{
    class GeneralHelper
    {
        public static bool IsPhone => DeviceFamilyOfCurrentDevice == DeviceFamily.Mobile;

        public static DeviceFamily DeviceFamilyOfCurrentDevice
            => (DeviceFamily)Enum.Parse(typeof(DeviceFamily), AnalyticsInfo.VersionInfo.DeviceFamily.Replace("Windows.", string.Empty));
    }
}
