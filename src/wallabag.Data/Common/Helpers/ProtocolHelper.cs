using System.Text.RegularExpressions;
using wallabag.Data.Models;

namespace wallabag.Data.Common.Helpers
{
    public static class ProtocolHelper
    {
        private static Regex _protocolRegex => new Regex("wallabag://(?<username>[a-zA-Z ]+)@(?<server>[a-zA-Z://.]+)");

        public static ProtocolSetupNavigationParameter Parse(string str)
        {
            ProtocolSetupNavigationParameter result = null;

            var match = _protocolRegex.Match(str);

            if (match.Success)
            {
                string user = match.Groups["username"].Value;
                string server = match.Groups["server"].Value
                    .Replace("https//", "https://")
                    .Replace("http//", "http://");

                result = new ProtocolSetupNavigationParameter(user, server);
            }

            return result;
        }

        public static bool Validate(string str) => _protocolRegex.Match(str).Success;
    }
}
