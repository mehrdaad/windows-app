using wallabag.Data.Models;
using wallabag.Data.ViewModels;
using Windows.UI.Xaml.Controls;

namespace wallabag.Controls
{
    public sealed partial class TagsControl : UserControl
    {
        public EditTagsViewModel ViewModel => DataContext as EditTagsViewModel;

        public TagsControl() => InitializeComponent();

        private void TagsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ViewModel.Tags.Remove(e.ClickedItem as Tag);
            ViewModel.RaisePropertyChanged(nameof(ViewModel.TagsCountIsZero));
            ViewModel.UpdateSuggestions();
        }
        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ViewModel.TagSubmittedCommand.Execute(e.ClickedItem);
            TagQueryTextBox.Focus(Windows.UI.Xaml.FocusState.Programmatic);
        }

        private void TagQueryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.TagQuery = (sender as TextBox).Text;
            ViewModel.TagQueryChangedCommand.Execute(null);
        }

        private void TagsListView_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
            => TagQueryTextBox.Focus(Windows.UI.Xaml.FocusState.Programmatic);
        private void AutoSuggestBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            // Checks if the comma key was pressed (code 188)
            if ((int)e.Key == 188 || e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;

                ViewModel.TagSubmittedCommand.Execute(null);

                (sender as TextBox).Text = string.Empty;
            }
            else if (e.Key == Windows.System.VirtualKey.Up &&
                ViewModel.Tags.Count > 0)
                TagsListView.Focus(Windows.UI.Xaml.FocusState.Programmatic);
            else if (e.Key == Windows.System.VirtualKey.Down &&
                ViewModel.Suggestions.Count > 0)
                SuggestionListView.Focus(Windows.UI.Xaml.FocusState.Programmatic);
        }
        private void SuggestionListView_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            var listView = sender as ListView;
            if (e.Key == Windows.System.VirtualKey.Up && listView.SelectedIndex <= 0)
                TagQueryTextBox.Focus(Windows.UI.Xaml.FocusState.Programmatic);
        }
    }
}
