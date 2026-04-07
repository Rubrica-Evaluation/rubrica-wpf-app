using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using GradingTool.Controls;
using GradingTool.Views;

namespace GradingTool.Services;

public class DialogService : IDialogService
{
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
            ("Non", false), ("Oui", true));
        dialog.ShowDialog();
        return dialog.ClickedButtonIndex == 1;
    }

    public OverwriteChoice ShowOverwriteConfirmation(int existingCount, int totalCount)
    {
        string message = $"{existingCount} fichier(s) sur {totalCount} existe(nt) déjà.\n\n"
                       + "Voulez-vous écraser les fichiers existants, les ignorer ou annuler l'opération ?";
        var dialog = new CustomDialog("Fichiers existants", message, CustomDialogIcon.Question,
            ("Annuler", false), ("Ignorer", false), ("Écraser", true));
        dialog.ShowDialog();

        if (dialog.ClickedButtonIndex != 2)
            return dialog.ClickedButtonIndex == 1 ? OverwriteChoice.Skip : OverwriteChoice.Cancel;

        var confirm = new CustomDialog(
            "Confirmation d'écrasement",
            "Attention : toutes les données saisies dans les grilles existantes seront définitivement perdues.\n\nCette action est irréversible. Confirmer ?",
            CustomDialogIcon.Warning,
            ("Annuler", false), ("Confirmer l'écrasement", true));
        confirm.ShowDialog();

        return confirm.ClickedButtonIndex == 1 ? OverwriteChoice.Overwrite : OverwriteChoice.Cancel;
    }

    public UnsavedChangesChoice ShowUnsavedChangesConfirmation(string context)
    {
        string message = $"Des modifications non enregistrées sont en cours dans {context}.\n\nVoulez-vous enregistrer avant de quitter ?";
        var dialog = new CustomDialog("Modifications non enregistrées", message, CustomDialogIcon.Warning,
            ("Annuler", false), ("Quitter sans enregistrer", false), ("Enregistrer", true));
        dialog.ShowDialog();
        return dialog.ClickedButtonIndex switch
        {
            2 => UnsavedChangesChoice.Save,
            1 => UnsavedChangesChoice.Discard,
            _ => UnsavedChangesChoice.Cancel
        };
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
