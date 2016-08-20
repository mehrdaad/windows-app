using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using wallabag.Common;
using wallabag.Models;
using wallabag.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace wallabag.Controls
{
    public sealed partial class TagsControl : UserControl
    {
        #region Dependency Properties

        public IEnumerable<Tag> ItemsSource
        {
            get { return (IEnumerable<Tag>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable<Tag>), typeof(TagsControl), new PropertyMetadata(new ObservableCollection<Tag>()));

        #endregion

        public ObservableCollection<Tag> Suggestions { get; set; } = new ObservableCollection<Tag>();

        public TagsControl()
        {
            this.InitializeComponent();
            this.Loaded += (s, e) => UpdateNoTagsInfoTextBlockVisibility();

            autoSuggestBox.KeyDown += AutoSuggestBox_KeyDown;
        }

        private bool _queryIsAlreadyHandled;
        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var itemsSource = ItemsSource as ICollection<Tag>;
            var suggestion = args.ChosenSuggestion as Tag;

            if (suggestion != null && !itemsSource.Contains(suggestion))
                itemsSource.Add(args.ChosenSuggestion as Tag);
            else
            {
                var tags = args.QueryText.Split(","[0]).ToList();
                foreach (var item in tags)
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        var newTag = new Tag() { Label = item, Id = itemsSource.Count + 1 };

                        if (itemsSource.Contains(newTag) == false)
                            itemsSource.Add(newTag);
                    }
            }

            UpdateNoTagsInfoTextBlockVisibility();
            if (string.IsNullOrEmpty(sender.Text))
                _queryIsAlreadyHandled = true;
            else
                sender.Text = string.Empty;
        }
        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var suggestionString = sender.Text.ToLower().Split(","[0]).Last();
                Suggestions.Replace(
                    App.Database.Table<Tag>().Where(t => t.Label.ToLower().StartsWith(suggestionString))
                        .Except(ItemsSource)
                        .Take(3)
                        .ToList());
            }
            else if (_queryIsAlreadyHandled)
            {
                _queryIsAlreadyHandled = false;
                sender.Text = string.Empty;
            }
        }

        private void UpdateNoTagsInfoTextBlockVisibility()
        {
            if (ItemsSource.Count() == 0)
                noTagsInfoTextBlock.Visibility = Visibility.Visible;
            else
                noTagsInfoTextBlock.Visibility = Visibility.Collapsed;
        }

        private void tagsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            (ItemsSource as IList<Tag>).Remove(e.ClickedItem as Tag);
            UpdateNoTagsInfoTextBlockVisibility();
        }

        private void AutoSuggestBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            // Checks if the comma key was pressed (code 188)
            if ((int)e.Key == 188)
            {
                e.Handled = true;
                var textBox = e.OriginalSource as TextBox;

                var label = textBox.Text.Replace(",", string.Empty);
                if (!string.IsNullOrWhiteSpace(label))
                    (ItemsSource as ObservableCollection<Tag>).Add(new Tag() { Label = label });

                textBox.Text = string.Empty;
            }
            UpdateNoTagsInfoTextBlockVisibility();
        }
    }
}
