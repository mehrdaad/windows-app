using GalaSoft.MvvmLight.Ioc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wallabag.Data.Common;
using wallabag.Data.Services;
using wallabag.Dialogs;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace wallabag.Services
{
    class NavigationService : INavigationService
    {
        private readonly Dictionary<Navigation.Pages, Type> _keys;
        private ILoggingService _logging => SimpleIoc.Default.GetInstance<ILoggingService>();
        private ISettingsService _settingService => SimpleIoc.Default.GetInstance<ISettingsService>();

        private Frame _frame => Window.Current.Content as Frame;
        private object _currentParameter;
        private bool _dialogIsOpen = false;
        private ContentDialog _currentDialog;

        public NavigationService()
        {
            _keys = new Dictionary<Navigation.Pages, Type>();

            _frame.RegisterPropertyChangedCallback(Frame.BackStackDepthProperty, (s, e) => UpdateShellBackButtonVisibility());
        }

        public void Configure(Navigation.Pages page, Type pageType) => _keys.Add(page, pageType);

        #region Navigation

        public void Navigate(Navigation.Pages page) => Navigate(page, null);
        public void Navigate(Navigation.Pages page, object parameter)
        {
            if (page == Navigation.Pages.AddItemPage)
                HandleDialogNavigationAsync(new AddItemDialog(), parameter).ConfigureAwait(true);
            else if (page == Navigation.Pages.EditTagsPage)
                HandleDialogNavigationAsync(new EditTagsDialog(), parameter).ConfigureAwait(true);
            else
                HandlePageNavigationAsync(() =>
                {
                    var pageType = _keys[page];

                    _logging.WriteLine($"Opening {pageType.Name} with parameter: {parameter}");
                    _frame.Navigate(pageType, parameter, new Windows.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                }, parameter).ConfigureAwait(true);
        }

        public void GoBack()
        {
            if (_frame.CanGoBack && !_dialogIsOpen)
            {
                _logging.WriteLine("Navigating one step back.");

                HandlePageNavigationAsync(() => _frame.GoBack()).ConfigureAwait(true);
            }
            else if (_dialogIsOpen)
            {
                _logging.WriteLine("Hiding current dialog.");
                _currentDialog.Hide();
            }
        }
        public void ClearHistory()
        {
            _frame.BackStack.Clear();
            UpdateShellBackButtonVisibility();
        }

        private Task HandleDialogNavigationAsync(ContentDialog dialog, object parameter)
        {
            _logging.WriteLine($"Opening {dialog.GetType().Name} with parameter: {parameter}");
            _currentDialog = dialog;

            dialog.Opened += async (s, e) =>
            {
                _logging.WriteLine($"Opened {s.GetType().Name}.");
                _dialogIsOpen = true;
                await HandleOnNavigatedToAsync(dialog.DataContext as INavigable, parameter);
            };
            dialog.Closed += (s, e) =>
            {
                _logging.WriteLine($"Closed {s.GetType().Name}.");
                _dialogIsOpen = false;
            };

            return dialog.ShowAsync().AsTask();
        }
        private async Task HandlePageNavigationAsync(Action navigationAction, object parameter = null)
        {
            var oldViewModel = (_frame.Content as Page)?.DataContext as INavigable;

            navigationAction.Invoke();

            if (oldViewModel != null)
                await HandleOnNavigatedFromAsync(oldViewModel);

            var newPage = _frame.Content as Page;

            bool ignoreNavigationCacheMode = oldViewModel == null;
            if (newPage.DataContext is INavigable newViewModel &&
                (ignoreNavigationCacheMode || newPage.NavigationCacheMode != Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled))
                await HandleOnNavigatedToAsync(newViewModel, parameter);

            UpdateShellBackButtonVisibility();
        }

        private Task HandleOnNavigatedToAsync(INavigable viewModel, object parameter)
        {
            string viewModelName = viewModel.GetType().Name;
            _logging.WriteLine($"Executing {nameof(INavigable.OnNavigatedToAsync)} from new ViewModel ({viewModelName}) with parameter: {parameter}");

            _currentParameter = parameter;

            return viewModel.OnNavigatedToAsync(parameter, GetSuspensionStateForPage(viewModelName));
        }

        private async Task HandleOnNavigatedFromAsync(INavigable viewModel)
        {
            string viewModelName = viewModel.GetType().Name;
            _logging.WriteLine($"Executing {nameof(INavigable.OnNavigatedFromAsync)} from new ViewModel ({viewModelName}).");

            var suspensionState = GetSuspensionStateForPage(viewModelName);
            await viewModel.OnNavigatedFromAsync(suspensionState);

            SetTimestampForSuspensionState(suspensionState);
        }

        #endregion

        #region Suspension

        private readonly string FrameNavigationState = "FrameNavigationState";
        private readonly string CurrentParameter = "SuspensionCurrentParameter";
        private readonly string SuspensionCacheDateKey = "Suspension.Cache.Date";

        public Task SaveAsync()
        {
            _logging.WriteLine("Saving navigation state.");

            _settingService.AddOrUpdateValue(FrameNavigationState, _frame.GetNavigationState());
            _settingService.AddOrUpdateValue(CurrentParameter, _currentParameter);

            return HandleOnNavigatedFromAsync((_frame.Content as Page).DataContext as INavigable);
        }
        public Task ResumeAsync()
        {
            _logging.WriteLine("Restoring current navigation state.");

            string navigationState = _settingService.GetValueOrDefault<string>(FrameNavigationState);
            if (!string.IsNullOrEmpty(navigationState))
                _frame.SetNavigationState(navigationState);

            _currentParameter = _settingService.GetValueOrDefault<object>(CurrentParameter);

            return HandleOnNavigatedToAsync((_frame.Content as Page).DataContext as INavigable, _currentParameter);
        }

        private void SetTimestampForSuspensionState(IDictionary<string, object> suspensionState)
            => suspensionState[SuspensionCacheDateKey] = DateTime.Now.ToString();

        private IDictionary<string, object> GetSuspensionStateForPage(string viewModelName)
        {
            _logging.WriteLine($"Returning suspension state for {viewModelName}");

            string pageKey = $"{viewModelName}-SuspensionState";
            var values = Settings.SettingsService.GetContainer(pageKey);

            if (values.ContainsKey(SuspensionCacheDateKey))
            {
                if (DateTime.TryParse(values[SuspensionCacheDateKey]?.ToString(), out var age))
                {
                    // Page cache will expire after three days
                    var setting = TimeSpan.FromDays(3);
                    var expires = DateTime.Now.Subtract(setting);
                    bool expired = expires <= age;

                    if (expired)
                    {
                        values.Clear();
                        SetTimestampForSuspensionState(values);
                    }
                    // else happiness
                }
                else
                    SetTimestampForSuspensionState(values);
            }
            else
                SetTimestampForSuspensionState(values);

            return values;
        }

        #endregion

        private void UpdateShellBackButtonVisibility()
            => SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility
                = _frame.CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
    }
}
