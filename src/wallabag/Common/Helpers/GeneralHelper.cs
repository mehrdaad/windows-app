using Windows.ApplicationModel.Resources;

namespace wallabag.Common.Helpers
{
    class GeneralHelper
    {
        public static bool IsPhone
        {
            get
            {
                var qualifiers = Windows.ApplicationModel.Resources.Core.ResourceContext.GetForCurrentView().QualifierValues;
                if (qualifiers.ContainsKey("DeviceFamily") && qualifiers["DeviceFamily"] == "Mobile")
                    return true;
                else return false;
            }
        }

        public static string LocalizedResource(string resourceName)
        {
            return ResourceLoader.GetForCurrentView().GetString(resourceName.Replace(".", "/"));
        }

        public static bool InternetConnectionIsAvailable
        {
            get
            {
                var profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
                return profile?.GetNetworkConnectivityLevel() == Windows.Networking.Connectivity.NetworkConnectivityLevel.InternetAccess;
            }
        }
    }
}
