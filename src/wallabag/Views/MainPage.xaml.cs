using PropertyChanged;
using wallabag.Common;
using wallabag.ViewModels;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

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
        public SearchPropertiesViewModel SearchPropertiesViewModel { get { return new SearchPropertiesViewModel(ViewModel.CurrentSearchProperties); } }

        public MainPage()
        {
            this.InitializeComponent();
            ItemGridView.SelectionChanged += (s, e) =>
            {
                foreach (ItemViewModel item in e.AddedItems)
                    SelectionViewModel.Items.Add(item);
                foreach (ItemViewModel item in e.RemovedItems)
                    SelectionViewModel.Items.Remove(item);

                if (ItemGridView.SelectedItems.Count == 0) DisableMultipleSelection();
            };

            ShowSearchStoryboard.Completed += (s, e) =>
            {
                _isSearchVisible = true;
                searchBox.Focus(FocusState.Programmatic);
            };
            HideSearchStoryboard.Completed += (s, e) => _isSearchVisible = false;
            ShowFilterStoryboard.Completed += (s, e) => _isFilterVisible = true;
            HideFilterStoryboard.Completed += (s, e) => _isFilterVisible = false;

            ViewModel.CurrentSearchProperties.SearchCanceled += p =>
            {
                if (_isSearchVisible)
                    HideSearchStoryboard.Begin();
            };
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
        private bool _isFilterVisible;
        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isSearchVisible)
                ShowSearchStoryboard.Begin();
            else
                HideSearchStoryboard.Begin();
        }
        private void filterButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isFilterVisible)
                ShowFilterStoryboard.Begin();
            else
                HideFilterStoryboard.Begin();
        }

        private void overlayRectangle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            HideFilterStoryboard.Begin();
        }
        private void searchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (Window.Current.Bounds.Width < 720)
                HideSearchStoryboard.Begin();
        }

        #endregion
    }
}
