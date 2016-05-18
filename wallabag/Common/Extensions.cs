using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
