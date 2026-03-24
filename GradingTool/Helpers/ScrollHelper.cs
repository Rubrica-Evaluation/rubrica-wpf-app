using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GradingTool.Helpers;

/// <summary>
/// Attached property that bubbles MouseWheel events to the parent ScrollViewer
/// when the inner control has nothing left to scroll in the requested direction.
/// Usage: helpers:ScrollHelper.BubbleToParent="True"
/// </summary>
public static class ScrollHelper
{
    public static readonly DependencyProperty BubbleToParentProperty =
        DependencyProperty.RegisterAttached(
            "BubbleToParent",
            typeof(bool),
            typeof(ScrollHelper),
            new PropertyMetadata(false, OnBubbleToParentChanged));

    public static bool GetBubbleToParent(DependencyObject obj) =>
        (bool)obj.GetValue(BubbleToParentProperty);

    public static void SetBubbleToParent(DependencyObject obj, bool value) =>
        obj.SetValue(BubbleToParentProperty, value);

    private static void OnBubbleToParentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            if ((bool)e.NewValue)
                element.PreviewMouseWheel += OnPreviewMouseWheel;
            else
                element.PreviewMouseWheel -= OnPreviewMouseWheel;
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var inner = FindScrollViewer((DependencyObject)sender);

        bool atBoundary = inner == null
            || (e.Delta < 0 && inner.VerticalOffset >= inner.ScrollableHeight)
            || (e.Delta > 0 && inner.VerticalOffset <= 0);

        if (atBoundary)
        {
            e.Handled = true;
            var parent = FindParentScrollViewer((DependencyObject)sender);
            parent?.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender
            });
        }
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer sv) return sv;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var result = FindScrollViewer(child);
            if (result != null) return result;
        }
        return null;
    }

    private static ScrollViewer? FindParentScrollViewer(DependencyObject child)
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is ScrollViewer sv) return sv;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }
}
