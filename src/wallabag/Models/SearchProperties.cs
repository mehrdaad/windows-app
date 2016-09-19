using PropertyChanged;
using System.ComponentModel;
using System;

namespace wallabag.Models
{
    [ImplementPropertyChanged]
    public class SearchProperties : INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;

        public event SearchChangedEventHandler SearchCanceled;
        public event SearchChangedEventHandler SearchStarted;

        public delegate void SearchChangedEventHandler(SearchProperties p);
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

        /// <summary>
        /// 0: unread
        /// 1: favorites
        /// 2: archived
        /// 3: all
        /// </summary>
        public int ItemTypeIndex { get; set; }
        public bool? OrderAscending { get; set; } = false;
        public SearchPropertiesSortType SortType { get; set; }
        public Language Language { get; set; }
        public Tag Tag { get; set; }

        public SearchProperties()
        {
            Reset();
        }

        public enum SearchPropertiesSortType
        {
            ByCreationDate = 0,
            ByReadingTime = 1
        }

        internal void Reset()
        {
            Query = string.Empty;
            ItemTypeIndex = 0;
            OrderAscending = false;
            SortType = SearchPropertiesSortType.ByCreationDate;
            Language = null;
            Tag = null;
        }
        internal void Replace(SearchProperties searchProperties)
        {
            Query = searchProperties.Query;
            ItemTypeIndex = searchProperties.ItemTypeIndex;
            OrderAscending = searchProperties.OrderAscending;
            SortType = searchProperties.SortType;
            Language = searchProperties.Language;
            Tag = searchProperties.Tag;
        }
    }
}
