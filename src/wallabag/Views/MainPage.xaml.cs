using GalaSoft.MvvmLight.Messaging;
using PropertyChanged;
using System.Linq;
using System.Threading.Tasks;
using wallabag.Common.Helpers;
using wallabag.Common.Messages;
using wallabag.Controls;
using wallabag.Services;
using wallabag.ViewModels;
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
        private bool _isUndoChangesGridVisible = false;
        public MainViewModel ViewModel { get { return DataContext as MainViewModel; } }

        public MainPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;

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
            ShowSearchResultsStoryboard.Completed += (s, e) => ((MainPivot.SelectedItem as PivotItem).Content as AdaptiveGridView).Focus(FocusState.Programmatic);
            HideSearchStoryboard.Completed += (s, e) => _isSearchVisible = false;
            ShowUndoChangesGridStoryboard.Completed += (s, e) => _isUndoChangesGridVisible = true;
            HideUndoChangesGridStoryboard.Completed += (s, e) => _isUndoChangesGridVisible = false;

            ViewModel.CurrentSearchProperties.SearchCanceled += p =>
            {
                if (_isSearchVisible)
                    HideSearchStoryboard.Begin();
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Messenger.Default.Register<CompleteMultipleSelectionMessage>(this, message => DisableMultipleSelection(true));
            Messenger.Default.Register<ShowUndoPopupMessage>(this, async message =>
            {
                ShowUndoChangesGridStoryboard.Begin();

                var descriptionTextBlock = FindName(nameof(UndoGridDescriptionTextBlock)) as TextBlock;
                descriptionTextBlock.Text = $"{message.Action.ToString()} for {message.NumberOfItems} items"; // TODO: Translation

                await Task.Delay(SettingsService.Instance.UndoTimeout).ContinueWith(t =>
                {
                    if (_isUndoChangesGridVisible)
                        HideUndoChangesGridStoryboard.Begin();
                });
            });
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Messenger.Default.Unregister(this);
        }

        private void UndoChangesButton_Click(object sender, RoutedEventArgs e)
        {
            SQLiteConnectionHelper.UndoChanges();
            HideUndoChangesGridStoryboard.Begin();
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
        private void searchButton_Click(object sender, RoutedEventArgs e) => ShowSearchStoryboard.Begin();
        private void filterButton_Checked(object sender, RoutedEventArgs e) => ShowFilterStoryboard.Begin();
        private void filterButton_Unchecked(object sender, RoutedEventArgs e) => HideFilterStoryboard.Begin();

        private void overlayRectangle_PointerPressed(object sender, PointerRoutedEventArgs e) => filterButton.IsChecked = false;
        private void searchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => ShowSearchResultsStoryboard.Begin();
        private void CloseSearchButton_Click(object sender, RoutedEventArgs e) => HideSearchStoryboard.Begin();

        #endregion

        /// <summary>
        /// Using only one ItemGridView at the time reduces the used amount of RAM
        /// </summary>
        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lastPivotItem = (e.RemovedItems?.FirstOrDefault() ?? (sender as Pivot).Items.FirstOrDefault()) as PivotItem;
            var currentPivotItem = e.AddedItems?.FirstOrDefault() as PivotItem;

            var gridView = lastPivotItem.Content;

            lastPivotItem.Content = null;
            currentPivotItem.Content = gridView;
        }
    }
}
