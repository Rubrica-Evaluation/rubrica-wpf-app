using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GradingTool.Views;

public enum CustomDialogIcon { Info, Warning, Error, Question }

public partial class CustomDialog : Window
{
    public int ClickedButtonIndex { get; private set; } = -1;

    public CustomDialog(string title, string message, CustomDialogIcon icon, params (string Label, bool IsPrimary)[] buttons)
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        Title = title;
        MessageText.Text = message;
        ApplyIcon(icon);

        for (int i = 0; i < buttons.Length; i++)
        {
            var (label, isPrimary) = buttons[i];
            var index = i;
            var btn = new Button
            {
                Content = label,
                Style = isPrimary
                    ? (Style)FindResource("RoundedButton")
                    : (Style)FindResource("GhostButton"),
                Margin = new Thickness(i == 0 ? 0 : 8, 0, 0, 0),
                IsDefault = isPrimary && i == buttons.Length - 1
            };
            btn.Click += (_, _) =>
            {
                ClickedButtonIndex = index;
                DialogResult = true;
            };
            ButtonsPanel.Children.Add(btn);
        }
    }

    private void ApplyIcon(CustomDialogIcon icon)
    {
        var (glyph, color) = icon switch
        {
            CustomDialogIcon.Warning  => ("!", Color.FromRgb(0xF5, 0x7C, 0x00)),
            CustomDialogIcon.Error    => ("×", Color.FromRgb(0xD3, 0x2F, 0x2F)),
            CustomDialogIcon.Question => ("?", Color.FromRgb(0x15, 0x65, 0xC0)),
            _                         => ("i", Color.FromRgb(0x00, 0x78, 0xD4)),
        };
        IconText.Text = glyph;
        IconBorder.Background = new SolidColorBrush(color);
    }
}
