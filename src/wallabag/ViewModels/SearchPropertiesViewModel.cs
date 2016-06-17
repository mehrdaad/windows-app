using PropertyChanged;
using System.ComponentModel;
using wallabag.Models;
using static wallabag.Models.SearchProperties;

namespace wallabag.ViewModels
{
    [ImplementPropertyChanged]
    public class SearchPropertiesViewModel : INotifyPropertyChanged
    {
        public SearchProperties Model { get; set; }

        public bool? IsAllItemsFilterActive { get { return Model.ItemType == SearchPropertiesItemType.All; } }
        public bool? IsUnreadItemsFilterActive { get { return Model.ItemType == SearchPropertiesItemType.Unread; } }
        public bool? IsStarredItemsFilterActive { get { return Model.ItemType == SearchPropertiesItemType.Favorites; } }
        public bool? IsArchivedItemsFilterActive { get { return Model.ItemType == SearchPropertiesItemType.Archived; } }

        public bool? SortAscendingByCreationDate { get { return Model.SortOrder == SearchPropertiesSortOrder.AscendingByCreationDate; } }
        public bool? SortDescendingByCreationDate { get { return Model.SortOrder == SearchPropertiesSortOrder.DescendingByCreationDate; } }
        public bool? SortAscendingByEstimatedReadingTime { get { return Model.SortOrder == SearchPropertiesSortOrder.AscendingByReadingTime; } }
        public bool? SortDescendingByEstimatedReadingTime { get { return Model.SortOrder == SearchPropertiesSortOrder.DescendingByReadingTime; } }

        public SearchPropertiesViewModel()
        {
            RegisterForPropertyChanges();
        }
        public SearchPropertiesViewModel(SearchProperties model)
        {
            this.Model = model;
            RegisterForPropertyChanges();
        }

        private void RegisterForPropertyChanges()
        {
            Model.SortOrderChanged += Model_SortOrderChanged;
            Model.ItemTypeChanged += Model_ItemTypeChanged;
        }
        private void Model_ItemTypeChanged(SearchPropertiesItemType newItemType)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAllItemsFilterActive)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUnreadItemsFilterActive)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsStarredItemsFilterActive)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsArchivedItemsFilterActive)));
        }
        private void Model_SortOrderChanged(SearchPropertiesSortOrder newSortOrder)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SortAscendingByCreationDate)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SortDescendingByCreationDate)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SortAscendingByEstimatedReadingTime)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SortDescendingByEstimatedReadingTime)));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
