using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using GradingTool.Models;
using GradingTool.Services;
using GradingTool.ViewModels;

namespace GradingTool.Views;

public partial class CommentPickerDialog : Window
{
    private CommentPickerViewModel Vm => (CommentPickerViewModel)DataContext;

    public CommentPickerDialog(string criterionLabel, List<CommentEntry> comments, ICommentService commentService)
    {
        InitializeComponent();
        DataContext = new CommentPickerViewModel(criterionLabel, comments, commentService);
        Vm.CloseRequested += result =>
        {
            DialogResult = result;
            Close();
        };
        Vm.EditRequested += originalText =>
        {
            var result = InputDialog.Show("Modifier le commentaire :", "Modifier", originalText, multiline: true);
            if (result != null)
                Vm.ApplyEdit(result);
        };
    }

    private void CommentListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Vm.SelectedEntry != null)
        {
            DialogResult = true;
            Close();
        }
    }

    public static string? Show(string criterionLabel, List<CommentEntry> comments, ICommentService commentService)
    {
        var dialog = new CommentPickerDialog(criterionLabel, comments, commentService)
        {
            Owner = Application.Current.MainWindow
        };
        return dialog.ShowDialog() == true ? dialog.Vm.SelectedEntry?.Text : null;
    }
}
