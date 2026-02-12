using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace GradingTool.Controls;

public partial class ToastNotification : UserControl
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(ToastNotification));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ToastNotification()
    {
        InitializeComponent();
    }

    public void Show(int durationMs = 3000)
    {
        var storyboard = new Storyboard();

        // Animation d'apparition
        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(300)
        };
        Storyboard.SetTarget(fadeIn, this);
        Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));

        // Animation de disparition
        var fadeOut = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            BeginTime = TimeSpan.FromMilliseconds(durationMs)
        };
        Storyboard.SetTarget(fadeOut, this);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));

        storyboard.Children.Add(fadeIn);
        storyboard.Children.Add(fadeOut);

        storyboard.Completed += (s, e) =>
        {
            if (Parent is Panel panel)
            {
                panel.Children.Remove(this);
            }
        };

        storyboard.Begin();
    }
}
