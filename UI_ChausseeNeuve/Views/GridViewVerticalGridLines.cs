using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace UI_ChausseeNeuve.Views
{
    /// <summary>
    /// Adds continuous vertical grid lines to a ListView with GridView, from the header to the bottom.
    /// Usage: local:GridViewVerticalGridLines.IsEnabled="True" on a ListView.
    /// </summary>
    public static class GridViewVerticalGridLines
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(GridViewVerticalGridLines),
            new PropertyMetadata(false, OnIsEnabledChanged));

        private static readonly DependencyProperty AdornerRefProperty = DependencyProperty.RegisterAttached(
            "AdornerRef",
            typeof(VerticalLinesAdorner),
            typeof(GridViewVerticalGridLines),
            new PropertyMetadata(null));

        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        private static void SetAdornerRef(DependencyObject element, VerticalLinesAdorner? value) => element.SetValue(AdornerRefProperty, value);
        private static VerticalLinesAdorner? GetAdornerRef(DependencyObject element) => (VerticalLinesAdorner?)element.GetValue(AdornerRefProperty);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListView listView) return;

            if ((bool)e.NewValue)
            {
                listView.Loaded -= ListView_Loaded;
                listView.Loaded += ListView_Loaded;
                if (listView.IsLoaded)
                {
                    Attach(listView);
                }
            }
            else
            {
                Detach(listView);
            }
        }

        private static void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListView lv)
            {
                Attach(lv);
            }
        }

        private static void Attach(ListView lv)
        {
            if (GetAdornerRef(lv) != null) return;
            if (lv.View is not GridView gv) return;

            var layer = AdornerLayer.GetAdornerLayer(lv);
            if (layer == null) return;

            var adorner = new VerticalLinesAdorner(lv);
            layer.Add(adorner);
            SetAdornerRef(lv, adorner);

            lv.Unloaded -= ListView_Unloaded;
            lv.Unloaded += ListView_Unloaded;
        }

        private static void Detach(ListView lv)
        {
            var layer = AdornerLayer.GetAdornerLayer(lv);
            var adorner = GetAdornerRef(lv);
            if (adorner != null && layer != null)
            {
                layer.Remove(adorner);
                SetAdornerRef(lv, null);
                adorner.Dispose();
            }
        }

        private static void ListView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListView lv)
            {
                Detach(lv);
            }
        }

        private sealed class VerticalLinesAdorner : Adorner, IDisposable
        {
            private readonly ListView _listView;
            private readonly GridView _gridView;
            private ScrollViewer? _scrollViewer;
            private readonly Pen _pen;
            private readonly List<DependencyPropertyDescriptor> _subscriptions = new();

            public VerticalLinesAdorner(ListView adornedElement) : base(adornedElement)
            {
                _listView = adornedElement;
                _gridView = (GridView)_listView.View!;

                // Use white separators (theme resource fallback)
                var brush = TryFindResource(_listView, "GridSeparatorBrush") as Brush
                            ?? TryFindResource(_listView, "TextWhite") as Brush
                            ?? Brushes.White;
                _pen = new Pen(brush, 1.0);
                _pen.Freeze();

                _listView.SizeChanged += OnInvalidate;

                // Find ScrollViewer for horizontal offset
                _scrollViewer = FindVisualChild<ScrollViewer>(_listView);
                if (_scrollViewer != null)
                {
                    _scrollViewer.ScrollChanged += OnScrollChanged;
                }

                HookColumns();
            }

            private static object? TryFindResource(FrameworkElement fe, object key)
            {
                return fe.TryFindResource(key) ?? Application.Current.TryFindResource(key);
            }

            private void HookColumns()
            {
                foreach (var col in _gridView.Columns)
                {
                    HookColumn(col);
                }

                _gridView.Columns.CollectionChanged += Columns_CollectionChanged;
            }

            private void Columns_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        if (item is GridViewColumn oldCol)
                        {
                            UnhookColumn(oldCol);
                        }
                    }
                }
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        if (item is GridViewColumn newCol)
                        {
                            HookColumn(newCol);
                        }
                    }
                }
                InvalidateVisual();
            }

            private void HookColumn(GridViewColumn column)
            {
                var dpd = DependencyPropertyDescriptor.FromProperty(GridViewColumn.WidthProperty, typeof(GridViewColumn));
                if (dpd != null)
                {
                    dpd.AddValueChanged(column, OnColumnWidthChanged);
                    _subscriptions.Add(dpd);
                }
            }

            private void UnhookColumn(GridViewColumn column)
            {
                var dpd = DependencyPropertyDescriptor.FromProperty(GridViewColumn.WidthProperty, typeof(GridViewColumn));
                if (dpd != null)
                {
                    dpd.RemoveValueChanged(column, OnColumnWidthChanged);
                }
            }

            private void OnColumnWidthChanged(object? sender, EventArgs e)
            {
                InvalidateVisual();
            }

            private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
            {
                if (e.HorizontalChange != 0 || e.ViewportWidthChange != 0)
                {
                    InvalidateVisual();
                }
            }

            private void OnInvalidate(object? sender, SizeChangedEventArgs e)
            {
                InvalidateVisual();
            }

            protected override void OnRender(DrawingContext dc)
            {
                base.OnRender(dc);
                if (_gridView.Columns.Count == 0) return;

                double x = 0.0;
                double offset = _scrollViewer?.HorizontalOffset ?? 0.0;

                // Pixel align offset depends on thickness: odd -> 0.5, even -> 0
                double alignOffset = (Math.Abs(_pen.Thickness % 2.0) < 0.01) ? 0.0 : 0.5;

                // Draw a vertical line after each column except the last
                for (int i = 0; i < _gridView.Columns.Count - 1; i++)
                {
                    var col = _gridView.Columns[i];
                    double width = GetColumnWidth(col);
                    x += width;
                    double lineX = Math.Round(x - offset) + alignOffset;

                    if (lineX >= 0 && lineX <= ActualWidth)
                    {
                        dc.DrawLine(_pen, new Point(lineX, 0), new Point(lineX, ActualHeight));
                    }
                }
            }

            private static double GetColumnWidth(GridViewColumn col)
            {
                if (!double.IsNaN(col.Width) && col.Width > 0)
                    return col.Width;

                if (col.Header is FrameworkElement fe && fe.IsLoaded)
                {
                    return fe.ActualWidth;
                }
                return 0;
            }

            public void Dispose()
            {
                _listView.SizeChanged -= OnInvalidate;
                if (_scrollViewer != null)
                {
                    _scrollViewer.ScrollChanged -= OnScrollChanged;
                }
                _gridView.Columns.CollectionChanged -= Columns_CollectionChanged;
                foreach (var col in _gridView.Columns)
                {
                    UnhookColumn(col);
                }
            }

            private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (child is T result)
                        return result;

                    var childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
                return null;
            }
        }
    }
}
