using PropertyChanged;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using wallabag.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
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
            this.InitializeComponent();
            ItemGridView.SelectionChanged += (s, e) =>
            {
                foreach (ItemViewModel item in e.AddedItems)
                    SelectionViewModel.Items.Add(item);
                foreach (ItemViewModel item in e.RemovedItems)
                    SelectionViewModel.Items.Remove(item);

                if (ItemGridView.SelectedItems.Count == 0) DisableMultipleSelection();
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
            ItemGridView.SelectionMode = ListViewSelectionMode.Multiple;
            MultipleSelectionCommandBar.Visibility = Visibility.Visible;
            ItemGridView.IsItemClickEnabled = false;
            ItemGridView.IsMultiSelectCheckBoxEnabled = true;
        }
        private void DisableMultipleSelection(bool forceDisable = false)
        {
            if (ItemGridView.SelectedItems.Count == 0 || forceDisable)
            {
                _IsMultipleSelectionEnabled = false;
                MultipleSelectionCommandBar.Visibility = Visibility.Collapsed;
                ItemGridView.SelectionMode = ListViewSelectionMode.None;
                ItemGridView.IsItemClickEnabled = true;
                ItemGridView.IsMultiSelectCheckBoxEnabled = false;
            }
        }

        private void EnableMultipleSelectionButtonClick(object sender, RoutedEventArgs e) => EnableMultipleSelection();
        private void DisableMultipleSelectionButtonClick(object sender, RoutedEventArgs e) => DisableMultipleSelection(true);
        #endregion
    }
}
