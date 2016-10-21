using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace wallabag.Common.Helpers
{
    public static class StringHelper
    {
        public static string FormatWith(this string format, object source)
        {
            if (format == null)
                throw new ArgumentNullException(nameof(format));

            Regex r = new Regex(@"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+",
                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            string rewrittenFormat = r.Replace(format, delegate (Match m)
            {
                Group startGroup = m.Groups["start"];
                Group propertyGroup = m.Groups["property"];
                Group formatGroup = m.Groups["format"];
                Group endGroup = m.Groups["end"];

                var value = (propertyGroup.Value == null)
                           ? source
                           : source.GetType().GetRuntimeProperty(propertyGroup.Value).GetValue(source);

                return value.ToString();
            });
            return rewrittenFormat;
        }

        public static bool IsValidUri(this string uriString)
        {
            try
            {
                Uri x = new Uri(uriString);
                return true;
            }
            catch (UriFormatException) { return false; }
        }
    }
}

