using wallabag.Models;

namespace wallabag.Common.Helpers
{
    public static class ProtocolHelper
    {
        private  const string PROTOCOL_HANDLER = "wallabag://";
        public static ProtocolSetupNavigationParameter Parse(string str)
        {
            ProtocolSetupNavigationParameter result = null;

            if (str.StartsWith(PROTOCOL_HANDLER))
                str = str.Remove(0, PROTOCOL_HANDLER.Length);

            var split = str.Split("@"[0]);

            if (split.Length == 2)
                result = new ProtocolSetupNavigationParameter(split[0], split[1].Replace("https//", "https://").Replace("http//", "http://"));

            return result;
        }
    }
}
