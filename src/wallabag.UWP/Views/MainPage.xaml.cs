﻿using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Graphics.Canvas.Effects;
using PropertyChanged;
using System;
using System.Linq;
using System.Numerics;
using wallabag.Common.Helpers;
using wallabag.Controls;
using wallabag.Data.Common.Messages;
using wallabag.Data.ViewModels;
using wallabag.Dialogs;
using Windows.Foundation;
using Windows.Graphics.Effects;
using Windows.System;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
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
        private ContentDialog _loginDialog;
        private bool _loginDialogIsOpen = false;
        public MainViewModel ViewModel => DataContext as MainViewModel;

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

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await TitleBarHelper.ResetToDefaultsAsync();

            var titleBarColor = (App.Current.Resources["SystemControlBackgroundChromeMediumBrush"] as SolidColorBrush).Color;
            TitleBarHelper.SetBackgroundColor(titleBarColor);
            TitleBarHelper.SetButtonBackgroundColor(titleBarColor);

            Messenger.Default.Register<CompleteMultipleSelectionMessage>(this, message => DisableMultipleSelection(true));
            Messenger.Default.Register<ShowLoginMessage>(this, async message =>
            {
                if (_loginDialog == null)
                {
                    _loginDialog = new LoginDialog();
                    _loginDialog.Opened += (s, args) => _loginDialogIsOpen = true;
                    _loginDialog.Closed += (s, args) => _loginDialogIsOpen = false;
                }

                if (!_loginDialogIsOpen)
                    await _loginDialog.ShowAsync();
            });
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Messenger.Default.Unregister(this);
        }

        #region Context menu
        private bool _isShiftPressed = false;
        private bool _isPointerPressed = false;
        private ItemViewModel _lastFocusedItemViewModel;

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Control)
                EnableMultipleSelection();

            if (e.Key == VirtualKey.Shift)
                _isShiftPressed = true;

            // Shift+F10 or the 'Menu' key next to Right Ctrl on most keyboards
            else if (_isShiftPressed && e.Key == VirtualKey.F10
                    || e.Key == VirtualKey.Application)
            {
                var FocusedUIElement = FocusManager.GetFocusedElement() as UIElement;
                if (FocusedUIElement is ContentControl)
                    _lastFocusedItemViewModel = ((ContentControl)FocusedUIElement).Content as ItemViewModel;

                ShowContextMenu(FocusedUIElement, new Point(0, 0));
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }
        protected override void OnKeyUp(KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Shift)
                _isShiftPressed = false;
            else if (e.Key == VirtualKey.Control)
                DisableMultipleSelection();

            base.OnKeyUp(e);
        }
        protected override void OnHolding(HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == Windows.UI.Input.HoldingState.Started)
            {
                _lastFocusedItemViewModel = (e.OriginalSource as FrameworkElement).DataContext as ItemViewModel;
                ShowContextMenu(null, e.GetPosition(null));
                e.Handled = true;

                _isPointerPressed = false;

                var itemsToCancel = VisualTreeHelper.FindElementsInHostCoordinates(e.GetPosition(null), ItemGridView);
                foreach (var item in itemsToCancel)
                    item.CancelDirectManipulations();
            }

            base.OnHolding(e);
        }
        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            _isPointerPressed = true;

            base.OnPointerPressed(e);
        }
        protected override void OnRightTapped(RightTappedRoutedEventArgs e)
        {
            if (_isPointerPressed)
            {
                _lastFocusedItemViewModel = (e.OriginalSource as FrameworkElement).DataContext as ItemViewModel;
                ShowContextMenu(null, e.GetPosition(null));

                e.Handled = true;
            }

            base.OnRightTapped(e);
        }
        private void ShowContextMenu(UIElement target, Point offset)
        {
            if (_lastFocusedItemViewModel != null && !_isMultipleSelectionEnabled)
            {
                var myFlyout = Resources["ItemContextMenuMenuFlyout"] as MenuFlyout;

                foreach (var item in myFlyout.Items)
                    item.DataContext = _lastFocusedItemViewModel;

                myFlyout.ShowAt(target, offset);
            }
        }
        #endregion

        #region Multiple selection
        private bool _isMultipleSelectionEnabled = false;

        public MultipleSelectionViewModel SelectionViewModel { get; set; } = SimpleIoc.Default.GetInstance<MultipleSelectionViewModel>();

        private void EnableMultipleSelection()
        {
            _isMultipleSelectionEnabled = true;
            VisualStateManager.GoToState(this, nameof(MultipleSelectionEnabled), false);
        }
        private void DisableMultipleSelection(bool forceDisable = false)
        {
            if (ItemGridView.SelectedItems.Count == 0 || forceDisable)
            {
                _isMultipleSelectionEnabled = false;
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

        private void ItemGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem != null)
                ViewModel.ItemClickCommand.Execute(e.ClickedItem);
        }

        private void RootPanel_Loaded(object sender, RoutedEventArgs e)
        {
            var panel = sender as Grid;
            var compositor = ElementCompositionPreview.GetElementVisual(panel).Compositor;

            var blurHost = panel.FindName("blurHost") as Border;
            var image = panel.FindName("image") as Image;

            InitializeFrostedGlass(blurHost);
            InitializeImageZoomAnimation(panel, image);
        }

        private void InitializeImageZoomAnimation(Grid root, Image image)
        {
            root.Clip = new RectangleGeometry()
            {
                Rect = new Rect(0, 0, root.ActualWidth, root.ActualHeight)
            };

            var imageVisual = ElementCompositionPreview.GetElementVisual(image);
            var compositor = imageVisual.Compositor;

            if (imageVisual.CenterPoint.X == 0 && imageVisual.CenterPoint.Y == 0)
                imageVisual.CenterPoint = new Vector3((float)root.ActualWidth / 2, (float)root.ActualHeight / 2, 0f);

            root.PointerEntered += (s, e) =>
            {
                if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
                {
                    var animation = CreateScaleAnimation(true);
                    imageVisual.StartAnimation("Scale.X", animation);
                    imageVisual.StartAnimation("Scale.Y", animation);
                }
            };
            root.PointerExited += (s, e) =>
            {
                if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
                {
                    var animation = CreateScaleAnimation(false);
                    imageVisual.StartAnimation("Scale.X", animation);
                    imageVisual.StartAnimation("Scale.Y", animation);
                }
            };
            root.SizeChanged += (s, e) =>
        {
            root.Clip = new RectangleGeometry()
            {
                Rect = new Rect(0, 0, root.ActualWidth, root.ActualHeight)
            };
        };

            ScalarKeyFrameAnimation CreateScaleAnimation(bool show)
            {
                var scaleAnimation = compositor.CreateScalarKeyFrameAnimation();
                scaleAnimation.InsertKeyFrame(1f, show ? 1.1f : 1f);
                scaleAnimation.Duration = TimeSpan.FromMilliseconds(700);
                scaleAnimation.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;
                return scaleAnimation;
            }
        }
        private void InitializeFrostedGlass(Border blurHost)
        {
            var compositor = ElementCompositionPreview.GetElementVisual(blurHost).Compositor;

            // Create a frosty glass effect
            var frostEffect = new GaussianBlurEffect
            {
                BlurAmount = 5.0f,
                BorderMode = EffectBorderMode.Hard,
                Source = new ArithmeticCompositeEffect
                {
                    MultiplyAmount = 0,
                    Source1Amount = 0.2f,
                    Source2Amount = 0.8f,
                    Source1 = new CompositionEffectSourceParameter("backdropBrush"),
                    Source2 = new ColorSourceEffect
                    {
                        Color = (Color)blurHost.Resources["SystemChromeMediumColor"]
                    }
                }
            };

            // Create an instance of the effect and set its source to a CompositionBackdropBrush
            var effectFactory = compositor.CreateEffectFactory(frostEffect);
            var backdropBrush = compositor.CreateBackdropBrush();
            var effectBrush = effectFactory.CreateBrush();

            effectBrush.SetSourceParameter("backdropBrush", backdropBrush);

            var frostVisual = compositor.CreateSpriteVisual();
            frostVisual.Brush = effectBrush;

            // Add the blur as a child of the host in the visual tree
            ElementCompositionPreview.SetElementChildVisual(blurHost, frostVisual);

            // Make sure size of frost host and frost visual always stay in sync
            var bindSizeAnimation = compositor.CreateExpressionAnimation("hostVisual.Size");
            bindSizeAnimation.SetReferenceParameter("hostVisual", ElementCompositionPreview.GetElementVisual(blurHost));

            frostVisual.StartAnimation("Size", bindSizeAnimation);
        }
    }
}
