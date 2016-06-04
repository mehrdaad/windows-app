using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

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
    public static class DependencyObjectExtensions
    {
        public static IEnumerable<T> FindChildren<T>(this DependencyObject d)           where T : DependencyObject
        {
            List<T> children = new List<T>();
            int childCount = VisualTreeHelper.GetChildrenCount(d);

            for (int i = 0; i < childCount; i++)
            {
                DependencyObject o = VisualTreeHelper.GetChild(d, i);

                if (o is T)
                    children.Add(o as T);

                foreach (T c in o.FindChildren<T>())
                    children.Add(c);
            }

            return children;
        }
    }
}
