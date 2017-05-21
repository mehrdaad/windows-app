using wallabag.Data.Models;
using wallabag.Data.ViewModels;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

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
        }

        private void AutoSuggestBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            // Checks if the comma key was pressed (code 188)
            if ((int)e.Key == 188 || e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;

                ViewModel.TagSubmittedCommand.Execute(null);

                (sender as TextBox).Text = string.Empty;
            }
        }

        private void autoSuggestBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            ViewModel.TagQuery = (sender as TextBox).Text;
            ViewModel.TagQueryChangedCommand.Execute(null);
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
           => ViewModel.TagSubmittedCommand.Execute(e.ClickedItem);
    }
}
