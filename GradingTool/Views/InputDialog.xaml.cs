using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GradingTool.Models;

namespace GradingTool.Views;

public partial class InputDialog : Window
{
    public new string Title { get; set; } = "Entrée";
    public string Prompt { get; set; } = "";
    public string InputText { get; set; } = "";
    public bool AddedToBank { get; set; } = true;
    public Visibility AddToBankVisible { get; private set; } = Visibility.Collapsed;
    public Visibility SeverityVisible { get; private set; } = Visibility.Collapsed;
    public IEnumerable<CommentSeverity> SeverityValues { get; } = Enum.GetValues<CommentSeverity>();
    public CommentSeverity SelectedSeverity { get; set; } = CommentSeverity.Aucun;

    public InputDialog(string prompt, string title, string defaultValue = "", bool multiline = false, bool showAddToBank = false)
    {
        InitializeComponent();
        Title = title;
        Prompt = prompt;
        InputText = defaultValue;
        DataContext = this;

        if (showAddToBank)
        {
            AddToBankVisible = Visibility.Visible;
            SeverityVisible = Visibility.Visible;
        }

        if (multiline)
        {
            MinHeight = showAddToBank ? 330 : 280;
            InputTextBox.AcceptsReturn = false; // Enter submits via IsDefault
            InputTextBox.TextWrapping = TextWrapping.Wrap;
            InputTextBox.MinHeight = 80;
            InputTextBox.MaxHeight = 300;
            InputTextBox.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Return && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    int caret = InputTextBox.CaretIndex;
                    InputTextBox.Text = InputTextBox.Text.Insert(caret, Environment.NewLine);
                    InputTextBox.CaretIndex = caret + Environment.NewLine.Length;
                    e.Handled = true;
                }
            };
        }
        else if (showAddToBank)
        {
            MinHeight = 265;
        }

        Loaded += (s, e) =>
        {
            InputTextBox.Focus();
            InputTextBox.CaretIndex = InputTextBox.Text.Length;
        };
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public static string? Show(string prompt, string title, string defaultValue = "", bool multiline = false)
    {
        var dialog = new InputDialog(prompt, title, defaultValue, multiline)
        {
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog() == true ? dialog.InputText : null;
    }

    public static (string? Text, bool AddToBank, CommentSeverity Severity) ShowWithBankOption(string prompt, string title, string defaultValue = "", bool multiline = false)
    {
        var dialog = new InputDialog(prompt, title, defaultValue, multiline, showAddToBank: true)
        {
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog() == true
            ? (dialog.InputText, dialog.AddedToBank, dialog.SelectedSeverity)
            : (null, false, CommentSeverity.Aucun);
    }
}
