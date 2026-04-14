using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using GradingTool.Controls;
using GradingTool.Models;
using GradingTool.Views;

namespace GradingTool.Services;

public class DialogService : IDialogService
{
    private readonly ILocalizationService _localizationService;

    public DialogService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public string? SelectFolder(string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public string? SelectFile(string title, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string[]? SelectFiles(string title, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            Multiselect = true
        };

        return dialog.ShowDialog() == true ? dialog.FileNames : null;
    }

    public string? SaveFile(string title, string defaultFileName, string filter)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            FileName = defaultFileName,
            Filter = filter
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public void ShowMessage(string message, string title, MessageBoxImage icon = MessageBoxImage.Information)
    {
        var dialogIcon = icon switch
        {
            MessageBoxImage.Warning  => CustomDialogIcon.Warning,
            MessageBoxImage.Error    => CustomDialogIcon.Error,
            MessageBoxImage.Question => CustomDialogIcon.Question,
            _                        => CustomDialogIcon.Info
        };
        new CustomDialog(title, message, dialogIcon, ("OK", true)).ShowDialog();
    }

    public bool ShowConfirmation(string message, string title)
    {
        var dialog = new CustomDialog(title, message, CustomDialogIcon.Question,
            (_localizationService["Common_No"], false), (_localizationService["Common_Yes"], true));
        dialog.ShowDialog();
        return dialog.ClickedButtonIndex == 1;
    }

    public OverwriteChoice ShowOverwriteConfirmation(int existingCount, int totalCount)
    {
        string message = string.Format(_localizationService["Dialog_Overwrite_Body"], existingCount, totalCount);
        var dialog = new CustomDialog(_localizationService["Dialog_Overwrite_Title"], message, CustomDialogIcon.Question,
            (_localizationService["Common_Cancel"], false), (_localizationService["Common_Skip"], false), (_localizationService["Common_Overwrite"], true));
        dialog.ShowDialog();

        if (dialog.ClickedButtonIndex != 2)
            return dialog.ClickedButtonIndex == 1 ? OverwriteChoice.Skip : OverwriteChoice.Cancel;

        var confirm = new CustomDialog(
            _localizationService["Dialog_ConfirmOverwrite_Title"],
            _localizationService["Dialog_ConfirmOverwrite_Body"],
            CustomDialogIcon.Warning,
            (_localizationService["Common_Cancel"], false), (_localizationService["Common_ConfirmOverwrite"], true));
        confirm.ShowDialog();

        return confirm.ClickedButtonIndex == 1 ? OverwriteChoice.Overwrite : OverwriteChoice.Cancel;
    }

    public UnsavedChangesChoice ShowUnsavedChangesConfirmation(string context)
    {
        string message = string.Format(_localizationService["Dialog_UnsavedChanges_Body"], context);
        var dialog = new CustomDialog(_localizationService["Dialog_UnsavedChanges_Title"], message, CustomDialogIcon.Warning,
            (_localizationService["Common_Cancel"], false), (_localizationService["Common_DiscardChanges"], false), (_localizationService["Common_Save"], true));
        dialog.ShowDialog();
        return dialog.ClickedButtonIndex switch
        {
            2 => UnsavedChangesChoice.Save,
            1 => UnsavedChangesChoice.Discard,
            _ => UnsavedChangesChoice.Cancel
        };
    }

    public string? ShowGroupImportTargetDialog(string fileName, List<GroupModel> existingGroups, string? suggestedGroupCode = null, string? suggestedDisplayName = null)
    {
        var dialog = new GroupSelectionDialog(fileName, existingGroups, suggestedGroupCode, suggestedDisplayName);
        return dialog.ShowDialog() == true ? dialog.SelectedGroupCode : null;
    }

    public void ShowToast(string message, int durationMs = 3000)
    {
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow != null)
        {
            // Trouver le Grid racine dans le MainWindow
            var rootGrid = FindRootGrid(mainWindow.Content);
            if (rootGrid != null)
            {
                var toast = new ToastNotification { Message = message };
                
                // Positionner le toast en bas à droite
                toast.HorizontalAlignment = HorizontalAlignment.Right;
                toast.VerticalAlignment = VerticalAlignment.Bottom;
                toast.Margin = new Thickness(0, 0, 20, 20);
                
                // S'assurer que le toast s'étend sur toutes les lignes et colonnes du Grid
                Grid.SetRowSpan(toast, int.MaxValue);
                Grid.SetColumnSpan(toast, int.MaxValue);
                
                // Utiliser Panel.ZIndex pour afficher au-dessus du contenu
                Panel.SetZIndex(toast, 1000);
                
                rootGrid.Children.Add(toast);
                toast.Show(durationMs);
            }
        }
    }

    private Grid? FindRootGrid(object? element)
    {
        if (element is Grid grid)
            return grid;
        
        if (element is ContentControl contentControl)
            return FindRootGrid(contentControl.Content);
        
        if (element is Decorator decorator)
            return FindRootGrid(decorator.Child);
        
        return null;
    }
}
