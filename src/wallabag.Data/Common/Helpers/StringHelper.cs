using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace wallabag.Data.Common.Helpers
{
    public static class StringHelper
    {
        public static string FormatWith(this string format, object source)
        {
            if (format == null)
                throw new ArgumentNullException(nameof(format));

            var r = new Regex(@"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+",
                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            string rewrittenFormat = r.Replace(format, delegate (Match m)
            {
                var startGroup = m.Groups["start"];
                var propertyGroup = m.Groups["property"];
                var formatGroup = m.Groups["format"];
                var endGroup = m.Groups["end"];

                object value = (propertyGroup.Value == null)
                           ? source
                           : source.GetType().GetRuntimeProperty(propertyGroup.Value).GetValue(source);

                return value.ToString();
            });
            return rewrittenFormat;
        }
    }
}

