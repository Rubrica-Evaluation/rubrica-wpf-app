using System.Windows;

namespace GradingTool.Views;

public partial class InputDialog : Window
{
    public new string Title { get; set; } = "Entrée";
    public string Prompt { get; set; } = "";
    public string InputText { get; set; } = "";

    public InputDialog(string prompt, string title, string defaultValue = "")
    {
        InitializeComponent();
        Title = title;
        Prompt = prompt;
        InputText = defaultValue;
        DataContext = this;
        
        Loaded += (s, e) => InputTextBox.Focus();
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

    public static string? Show(string prompt, string title, string defaultValue = "")
    {
        var dialog = new InputDialog(prompt, title, defaultValue)
        {
            Owner = Application.Current.MainWindow
        };
        
        return dialog.ShowDialog() == true ? dialog.InputText : null;
    }
}
