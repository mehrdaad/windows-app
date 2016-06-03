using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wallabag.Models
{
    public class SearchProperties
    {
        public SearchPropertiesItemType? ItemType { get; set; }
        public SortOrder? ReadingTimeSortOrder { get; set; }
        public SortOrder? CreationDateSortOrder { get; set; }

        public enum SearchPropertiesItemType
        {
            All = 0,
            Unread = 1,
            Favorites = 2,
            Archived = 3
        }
        public enum SortOrder
        {
            Ascending = 0,
            Descending = 1
        }

        public SearchProperties()
        {
            ItemType = SearchPropertiesItemType.Unread;
            ReadingTimeSortOrder = null;
            CreationDateSortOrder = null;        
        }
    }
}
