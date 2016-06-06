using PropertyChanged;
using System.ComponentModel;

namespace wallabag.Models
{
    [ImplementPropertyChanged]
    public class SearchProperties : INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;

        public event SearchChangedEventHandler SearchCanceled;
        public event SearchChangedEventHandler SearchStarted;
        public event ItemTypeChangedEventHandler ItemTypeChanged;
        public event SortOrderChangedEventHandler SortOrderChanged;

        public delegate void SearchChangedEventHandler(SearchProperties p);
        public delegate void ItemTypeChangedEventHandler(SearchPropertiesItemType newItemType);
        public delegate void SortOrderChangedEventHandler(SearchPropertiesSortOrder newSortOrder);
        #endregion

        private string _query;
        public string Query
        {
            get { return _query; }
            set
            {
                var oldValue = _query;
                if (_query != value)
                {
                    _query = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Query)));
                }

                if (string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(oldValue))
                    SearchCanceled?.Invoke(this);

                if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(oldValue))
                    SearchStarted?.Invoke(this);
            }
        }

        private SearchPropertiesItemType _itemType = SearchPropertiesItemType.Unread;
        public SearchPropertiesItemType ItemType
        {
            get { return _itemType; }
            set
            {
                if (_itemType != value)
                {
                    _itemType = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ItemType)));
                    ItemTypeChanged?.Invoke(value);
                }
            }
        }

        private SearchPropertiesSortOrder _sortOrder = SearchPropertiesSortOrder.DescendingByCreationDate;
        public SearchPropertiesSortOrder SortOrder
        {
            get { return _sortOrder; }
            set
            {
                if (_sortOrder != value)
                {
                    _sortOrder = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SortOrder)));
                    SortOrderChanged?.Invoke(value);
                }
            }
        }
        public Language Language { get; set; }

        public enum SearchPropertiesItemType
        {
            All = 0,
            Unread = 1,
            Favorites = 2,
            Archived = 3
        }
        public enum SearchPropertiesSortOrder
        {
            DescendingByCreationDate = 0,
            AscendingByCreationDate = 1,
            DescendingByReadingTime = 2,
            AscendingByReadingTime = 3
        }

        public SearchProperties()
        {
            Reset();
        }

        public void Reset()
        {
            Query = string.Empty;
            ItemType = SearchPropertiesItemType.Unread;
            SortOrder = SearchPropertiesSortOrder.DescendingByCreationDate;
            Language = null;
        }
    }
}
