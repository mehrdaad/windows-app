using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace wallabag.Common
{
    class Helpers
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
