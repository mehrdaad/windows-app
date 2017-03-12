using wallabag.Data.Models;
using wallabag.Data.ViewModels;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace wallabag.Controls
{
    public sealed partial class TagsControl : UserControl
    {
        public EditTagsViewModel ViewModel => DataContext as EditTagsViewModel;

        public TagsControl()
        {
            InitializeComponent();

            autoSuggestBox.KeyDown += AutoSuggestBox_KeyDown;
        }

        private void TagsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ViewModel.Tags.Remove(e.ClickedItem as Tag);
            ViewModel.RaisePropertyChanged(nameof(ViewModel.TagsCountIsZero));
        }

        private void AutoSuggestBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            // Checks if the comma key was pressed (code 188)
            if ((int)e.Key == 188)
            {
                e.Handled = true;
                var textBox = e.OriginalSource as TextBox;

                string label = textBox.Text.Replace(",", string.Empty);
                if (!string.IsNullOrWhiteSpace(label))
                    ViewModel.Tags.Add(new Tag() { Label = label });

                textBox.Text = string.Empty;
            }
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
            => ViewModel.TagQueryChangedCommand.Execute(null);

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
            => ViewModel.TagSubmittedCommand.Execute(args.ChosenSuggestion);
    }
}
