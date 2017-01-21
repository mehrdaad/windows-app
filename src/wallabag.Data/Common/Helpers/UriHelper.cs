using System;

namespace wallabag.Data.Common.Helpers
{
    public static class UriHelper
    {
        public static Uri Append(this Uri baseUri, string relativeString)
        {
            if (baseUri.IsAbsoluteUri == false)
                throw new FormatException("The URI is not absolute.");

            string finalPath = baseUri.AbsolutePath;

            if (finalPath.EndsWith("/") == false)
                finalPath += "/";

            if (relativeString.StartsWith("/"))
                relativeString = relativeString.TrimStart('/');

            finalPath += relativeString;

            var result = new Uri(baseUri, finalPath);
            return result;
        }

        public static bool IsValidUri(this string uriString)
        {
            try
            {
                var x = new Uri(uriString);
                return true;
            }
            catch (UriFormatException) { return false; }
        }
    }
}

