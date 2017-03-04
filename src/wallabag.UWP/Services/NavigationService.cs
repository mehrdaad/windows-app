using GalaSoft.MvvmLight.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wallabag.Data.Common;
using wallabag.Data.Services;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using static wallabag.Data.Common.Navigation;

namespace wallabag.Services
{
    class NavigationService : INavigationService
    {
        private ILoggingService _loggingService => SimpleIoc.Default.GetInstance<ILoggingService>();
        private ISettingsService _settingsService => SimpleIoc.Default.GetInstance<ISettingsService>();

        private Dictionary<Pages, Type> _keys;

        public NavigationService()
        {
            _keys = new Dictionary<Pages, Type>();
        }

        public Frame Frame => Window.Current.Content as Frame;
        public Pages CurrentPage { get; private set; }
        public object CurrentParameter { get; private set; }

        public void ClearHistory()
        {
            Frame.BackStack.Clear();
            UpdateBackButtonVisibility();
        }
        internal void UpdateBackButtonVisibility() => SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = Frame.CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;

        public void GoBack()
        {
            if (Frame.CanGoBack)
                HandleNavigationAsync(null, navigateBack: true).ConfigureAwait(true);

            UpdateBackButtonVisibility();
        }
        public void Navigate(Pages pageKey) => Navigate(GetPageType(pageKey));
        public void Navigate(Pages pageKey, object parameter) => Navigate(GetPageType(pageKey), parameter);
        public void Navigate(Type page) => Navigate(page, null);
        public void Navigate(Type page, object parameter) => HandleNavigationAsync(page, parameter).ConfigureAwait(true);

        public void Configure(Pages pageKey, Type pageType) => _keys.Add(pageKey, pageType);

        private async Task HandleNavigationAsync(Type pageType, object parameter = null, bool navigateBack = false)
        {
            if (pageType == GetPageType(Pages.AddItemPage) && !navigateBack)
                await new Dialogs.AddItemDialog().ShowAsync();
            else if (pageType == GetPageType(Pages.EditTagsPage) && !navigateBack)
                await new Dialogs.EditTagsDialog().ShowAsync();
            else
            {
                _loggingService.WriteLine($"Navigating to {pageType}. Type of parameter: {parameter?.GetType()?.Name}");

                var oldPage = Frame.Content as Page;
                _loggingService.WriteLine("Starting navigation...");

                if (navigateBack)
                    Frame.GoBack();
                else
                    Frame.Navigate(pageType, parameter, new DrillInNavigationTransitionInfo());

                UpdateBackButtonVisibility();

                CurrentPage = _keys.Where(i => i.Value == pageType).First().Key;
                CurrentParameter = parameter;

                await HandleOnNavigatedFromAsync(oldPage);
                await HandleOnNavigatedToAsync(parameter);
            }
        }

        private async Task HandleOnNavigatedToAsync(object parameter)
        {
            // fetch (current which is now new)
            var newPage = Frame.Content as Page;
            var newViewModel = newPage?.DataContext as INavigable;

            if (newViewModel != null)
            {
                _loggingService.WriteLine($"Executing {nameof(INavigable.OnNavigatedToAsync)} from new ViewModel ({newViewModel?.GetType()?.Name ?? "NULL"}).");
                await newViewModel.OnNavigatedToAsync(parameter, GetPageStateForPage(newPage));
            }
            else _loggingService.WriteLine($"{nameof(INavigable.OnNavigatedToAsync)} wasn't executed because the ViewModel was null.");
        }

        private async Task HandleOnNavigatedFromAsync(Page oldPage)
        {
            var oldViewModel = oldPage?.DataContext as INavigable;
            if (oldViewModel != null)
            {
                _loggingService.WriteLine($"Executing {nameof(INavigable.OnNavigatedFromAsync)} from old ViewModel ({oldViewModel?.GetType()?.Name ?? "NULL"}).");
                await oldViewModel.OnNavigatedFromAsync(GetPageStateForPage(oldPage));
            }
            else _loggingService.WriteLine($"{nameof(INavigable.OnNavigatedFromAsync)} wasn't executed because the ViewModel was null.");
        }

        private IDictionary<string, object> GetPageStateForPage(Page page)
        {
            string pageKey = $"{page.GetType().FullName}-SuspensionState";
            return Settings.SettingsService.GetContainer(pageKey);
        }

        private Type GetPageType(Pages pageKey) => _keys.FirstOrDefault(i => i.Key == pageKey).Value;

        private const string m_NAVIGATIONSTATESTRING = "NavigationState";
        private const string m_SUSPENSIONSTATEPREFIX = "SuspensionState-";
        public Task SaveAsync()
        {
            _loggingService.WriteLine("Saving navigation state.");
            string navState = Frame.GetNavigationState();

            _settingsService.AddOrUpdateValue(m_NAVIGATIONSTATESTRING, navState);
            _settingsService.AddOrUpdateValue($"{m_SUSPENSIONSTATEPREFIX}{nameof(CurrentPage)}", CurrentPage);
            _settingsService.AddOrUpdateValue($"{m_SUSPENSIONSTATEPREFIX}{nameof(CurrentParameter)}", CurrentParameter);

            return HandleOnNavigatedFromAsync(Frame.Content as Page);
        }

        public Task ResumeAsync()
        {
            _loggingService.WriteLine("Restoring navigation state.");

            string navState = _settingsService.GetValueOrDefault(m_NAVIGATIONSTATESTRING, string.Empty);
            CurrentPage = _settingsService.GetValueOrDefault<Pages>($"{m_SUSPENSIONSTATEPREFIX}{nameof(CurrentPage)}");
            CurrentParameter = _settingsService.GetValueOrDefault<object>($"{m_SUSPENSIONSTATEPREFIX}{nameof(CurrentParameter)}");

            Frame.SetNavigationState(navState);

            return HandleOnNavigatedToAsync(CurrentParameter);
        }
    }
}
