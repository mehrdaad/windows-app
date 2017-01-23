using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System.Linq;
using wallabag.Controls;
using wallabag.Data.Common.Messages;
using wallabag.Data.ViewModels;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace wallabag.Views
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    [ImplementPropertyChanged]
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get { return DataContext as MainViewModel; } }

        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;

            ItemGridView.SelectionChanged += (s, e) =>
            {
                foreach (object item in e.AddedItems)
                    SelectionViewModel.Items.Add(item as ItemViewModel);
                foreach (object item in e.RemovedItems)
                    SelectionViewModel.Items.Remove(item as ItemViewModel);

                if (ItemGridView.SelectedItems.Count == 0) DisableMultipleSelection();
            };

            ShowSearchStoryboard.Completed += (s, e) =>
            {
                _isSearchVisible = true;
                searchBox.Focus(FocusState.Programmatic);
            };
            ShowSearchResultsStoryboard.Completed += (s, e) => ((MainPivot.SelectedItem as PivotItem).Content as AdaptiveGridView).Focus(FocusState.Programmatic);
            HideSearchStoryboard.Completed += (s, e) => _isSearchVisible = false;

            ViewModel.CurrentSearchProperties.SearchCanceled += (s, e) =>
            {
                if (_isSearchVisible)
                    HideSearchStoryboard.Begin();
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Messenger.Default.Register<CompleteMultipleSelectionMessage>(this, message => DisableMultipleSelection(true));
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Messenger.Default.Unregister(this);
        }

        #region Context menu
        private bool _IsShiftPressed = false;
        private bool _IsPointerPressed = false;
        private ItemViewModel _LastFocusedItemViewModel;

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Control)
                EnableMultipleSelection();

            if (e.Key == VirtualKey.Shift)
                _IsShiftPressed = true;

            // Shift+F10 or the 'Menu' key next to Right Ctrl on most keyboards
            else if (_IsShiftPressed && e.Key == VirtualKey.F10
                    || e.Key == VirtualKey.Application)
            {
                var FocusedUIElement = FocusManager.GetFocusedElement() as UIElement;
                if (FocusedUIElement is ContentControl)
                    _LastFocusedItemViewModel = ((ContentControl)FocusedUIElement).Content as ItemViewModel;

                ShowContextMenu(FocusedUIElement, new Point(0, 0));
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }
        protected override void OnKeyUp(KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Shift)
                _IsShiftPressed = false;
            else if (e.Key == VirtualKey.Control)
                DisableMultipleSelection();

            base.OnKeyUp(e);
        }
        protected override void OnHolding(HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == Windows.UI.Input.HoldingState.Started)
            {
                _LastFocusedItemViewModel = (e.OriginalSource as FrameworkElement).DataContext as ItemViewModel;
                ShowContextMenu(null, e.GetPosition(null));
                e.Handled = true;

                _IsPointerPressed = false;

                var itemsToCancel = VisualTreeHelper.FindElementsInHostCoordinates(e.GetPosition(null), ItemGridView);
                foreach (var item in itemsToCancel)
                    item.CancelDirectManipulations();
            }

            base.OnHolding(e);
        }
        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            _IsPointerPressed = true;

            base.OnPointerPressed(e);
        }
        protected override void OnRightTapped(RightTappedRoutedEventArgs e)
        {
            if (_IsPointerPressed)
            {
                _LastFocusedItemViewModel = (e.OriginalSource as FrameworkElement).DataContext as ItemViewModel;
                ShowContextMenu(null, e.GetPosition(null));

                e.Handled = true;
            }

            base.OnRightTapped(e);
        }
        private void ShowContextMenu(UIElement target, Point offset)
        {
            if (_LastFocusedItemViewModel != null && !_IsMultipleSelectionEnabled)
            {
                var myFlyout = Resources["ItemContextMenuMenuFlyout"] as MenuFlyout;

                foreach (var item in myFlyout.Items)
                    item.DataContext = _LastFocusedItemViewModel;

                myFlyout.ShowAt(target, offset);
            }
        }
        #endregion

        #region Multiple selection
        private bool _IsMultipleSelectionEnabled = false;

        public MultipleSelectionViewModel SelectionViewModel { get; set; } = new MultipleSelectionViewModel();

        private void EnableMultipleSelection()
        {
            _IsMultipleSelectionEnabled = true;
            VisualStateManager.GoToState(this, nameof(MultipleSelectionEnabled), false);
        }
        private void DisableMultipleSelection(bool forceDisable = false)
        {
            if (ItemGridView.SelectedItems.Count == 0 || forceDisable)
            {
                _IsMultipleSelectionEnabled = false;
                VisualStateManager.GoToState(this, nameof(MultipleSelectionDisabled), false);
            }
        }

        private void EnableMultipleSelectionButtonClick(object sender, RoutedEventArgs e) => EnableMultipleSelection();
        private void DisableMultipleSelectionButtonClick(object sender, RoutedEventArgs e) => DisableMultipleSelection(true);
        #endregion

        #region Search & Filter

        private bool _isSearchVisible;
        private void SearchButton_Click(object sender, RoutedEventArgs e) => ShowSearchStoryboard.Begin();
        private void FilterButton_Checked(object sender, RoutedEventArgs e) => ShowFilterStoryboard.Begin();
        private void FilterButton_Unchecked(object sender, RoutedEventArgs e) => HideFilterStoryboard.Begin();

        private void OverlayRectangle_PointerPressed(object sender, PointerRoutedEventArgs e) => filterButton.IsChecked = false;
        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => ShowSearchResultsStoryboard.Begin();
        private void CloseSearchButton_Click(object sender, RoutedEventArgs e) => HideSearchStoryboard.Begin();

        #endregion

        /// <summary>
        /// Using only one ItemGridView at the time reduces the used amount of RAM
        /// </summary>
        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lastPivotItem = (e.RemovedItems?.FirstOrDefault() ?? (sender as Pivot).Items.FirstOrDefault()) as PivotItem;
            var currentPivotItem = e.AddedItems?.FirstOrDefault() as PivotItem;

            object gridView = lastPivotItem.Content;

            lastPivotItem.Content = null;
            currentPivotItem.Content = gridView;
        }
    }
}
