using System.Collections.Generic;
using System.Linq;

namespace wallabag.Data.Common.Helpers
{
    public static class ListHelper
    {
        public static void AddSorted<T>(this IList<T> list, T item, Comparer<T> comparer = null, bool sortAscending = false)
        {
            if (list.Contains(item))
                return;

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

        public static void Replace<T>(this IList<T> oldList, IList<T> newList, bool withAnimations = false)
        {
            if (!withAnimations)
            {
                oldList.Clear();
                foreach (var item in newList)
                    oldList.Add(item);
            }
            else
            {
                var addedItems = newList.Except(oldList).ToList();
                var deletedItems = oldList.Except(newList).ToList();

                foreach (var item in addedItems)
                    oldList.AddSorted(item, sortAscending: true);

                foreach (var item in deletedItems.ToList())
                    oldList.Remove(item);
            }
        }

        public static string[] ToStringArray<T>(this IEnumerable<T> list) => string.Join(",", list).Split(","[0]);
    }

}
