using PropertyChanged;

namespace wallabag.Models
{
    [ImplementPropertyChanged]
    public class SearchProperties
    {
        public event SearchChangedHandler SearchCanceled;
        public event SearchChangedHandler SearchStarted;
        public delegate void SearchChangedHandler(SearchProperties p);

        private string _query;
        public string Query
        {
            get { return _query; }
            set
            {
                if (string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(_query))
                    SearchCanceled?.Invoke(this);

                if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(_query))
                    SearchStarted?.Invoke(this);

                if (value != _query)
                    _query = value;
            }
        }

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
            Query = string.Empty;
            ItemType = SearchPropertiesItemType.Unread;
            ReadingTimeSortOrder = null;
            CreationDateSortOrder = null;
        }
    }
}
