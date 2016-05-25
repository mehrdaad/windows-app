using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace wallabag.Common
{
    public static class IListExtensions
    {
        public static void AddSorted<T>(this IList<T> list, T item, Comparer<T> comparer = null, bool sortAscending = false)
        {
            if (comparer == null)
                comparer = Comparer<T>.Default;

            int i = 0;

            if (sortAscending)
                while (i < list.Count && comparer.Compare(list[i], item) < 0)
                    i++;
            else
                while (i < list.Count && comparer.Compare(list[i], item) > 0)
                    i++;

            list.Insert(i, item);
        }
        public static void Replace<T>(this IList<T> oldList, IList<T> newList)
        {
            oldList.Clear();
            foreach (var item in newList)
                oldList.Add(item);
        }
    }

    public static class StringExtensions
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
    }
}
