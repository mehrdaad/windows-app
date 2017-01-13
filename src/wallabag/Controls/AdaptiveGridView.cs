/* 
    Adapted from https://github.com/LanceMcCarthy/UwpProjects.
*/

using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace wallabag.Controls
{
    /// <summary>
    /// A GridView that lets you set a MinWidth and MinHeight for items you want to restrict to an aspect ration, but still resize
    /// </summary>
    public class AdaptiveGridView : GridView
    {
        #region DependencyProperties

        /// <summary>
        /// Minimum height for item
        /// </summary>
        public double MinItemHeight
        {
            get { return (double)GetValue(AdaptiveGridView.MinItemHeightProperty); }
            set { SetValue(AdaptiveGridView.MinItemHeightProperty, value); }
        }

        public static readonly DependencyProperty MinItemHeightProperty =
            DependencyProperty.Register(
                "MinItemHeight",
                typeof(double),
                typeof(AdaptiveGridView),
                new PropertyMetadata(1.0, (s, a) =>
                {
                    if (!double.IsNaN((double)a.NewValue))
                    {
                        ((AdaptiveGridView)s).InvalidateMeasure();
                    }
                }));

        /// <summary>
        /// Minimum width for item (must be greater than zero)
        /// </summary>
        public double MinItemWidth
        {
            get { return (double)GetValue(AdaptiveGridView.MinimumItemWidthProperty); }
            set { SetValue(AdaptiveGridView.MinimumItemWidthProperty, value); }
        }

        public static readonly DependencyProperty MinimumItemWidthProperty =
            DependencyProperty.Register(
                "MinItemWidth",
                typeof(double),
                typeof(AdaptiveGridView),
                new PropertyMetadata(1.0, (s, a) =>
                {
                    if (!double.IsNaN((double)a.NewValue))
                    {
                        ((AdaptiveGridView)s).InvalidateMeasure();
                    }
                }));

        public bool IsItemHeightLocked
        {
            get { return (bool)GetValue(IsHeightLockedProperty); }
            set { SetValue(IsHeightLockedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsHeightLocked.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsHeightLockedProperty =
            DependencyProperty.Register("IsItemHeightLocked", typeof(bool), typeof(AdaptiveGridView), new PropertyMetadata(false));

        public bool IsItemWidthLocked
        {
            get { return (bool)GetValue(IsWidthLockedProperty); }
            set { SetValue(IsWidthLockedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsWidthLocked.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsWidthLockedProperty =
            DependencyProperty.Register("IsItemWidthLocked", typeof(bool), typeof(AdaptiveGridView), new PropertyMetadata(false));

        #endregion

        public AdaptiveGridView()
        {
            if (ItemContainerStyle == null)
                ItemContainerStyle = new Style(typeof(GridViewItem));

            ItemContainerStyle.Setters.Add(new Setter(GridViewItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            ItemContainerStyle.Setters.Add(new Setter(GridViewItem.VerticalContentAlignmentProperty, VerticalAlignment.Stretch));

            this.Loaded += (s, a) =>
            {
                if (ItemsPanelRoot != null)
                    InvalidateMeasure();
            };
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var panel = ItemsPanelRoot as ItemsWrapGrid;
            if (panel != null)
            {
                if (MinItemWidth == 0)
                    throw new DivideByZeroException("You need to have a MinItemWidth greater than zero");

                double availableWidth = availableSize.Width - (Padding.Right + Padding.Left);

                double numColumns = Math.Floor(availableWidth / MinItemWidth);
                numColumns = numColumns == 0 ? 1 : numColumns;
                double numRows = Math.Ceiling(Items.Count / numColumns);

                double itemWidth = availableWidth / numColumns;
                double aspectRatio = MinItemHeight / MinItemWidth;
                double itemHeight = itemWidth * aspectRatio;

                if (IsItemWidthLocked)
                    itemWidth = MinItemWidth;
                if (IsItemHeightLocked)
                    itemHeight = MinItemHeight;

                panel.ItemWidth = itemWidth;
                panel.ItemHeight = itemHeight;
                panel.MaximumRowsOrColumns = (int)numColumns;
            }

            return base.MeasureOverride(availableSize);
        }
    }
}
