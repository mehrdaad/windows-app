using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using wallabag.Api.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace wallabag.Controls
{
    public sealed partial class TagsControl : UserControl
    {
        #region Dependency Properties

        public IEnumerable<WallabagTag> ItemsSource
        {
            get { return (IEnumerable<WallabagTag>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable<WallabagTag>), typeof(TagsControl), new PropertyMetadata(new ObservableCollection<WallabagTag>()));

        #endregion

        public TagsControl()
        {
            this.InitializeComponent();
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var tags = args.QueryText.Split(","[0]).ToList();
            var itemsSource = ItemsSource as ICollection<WallabagTag>;

            foreach (var item in tags)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    var newTag = new WallabagTag() { Label = item, Id = itemsSource.Count + 1 };
                    if (itemsSource.Contains(newTag) == false)
                        itemsSource.Add(newTag);
                }
            }
            sender.Text = string.Empty;
        }
    }
}
