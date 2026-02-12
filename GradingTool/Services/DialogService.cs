using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using GradingTool.Controls;

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
        MessageBox.Show(message, title, MessageBoxButton.OK, icon);
    }

    public bool ShowConfirmation(string message, string title)
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    public OverwriteChoice ShowOverwriteConfirmation(int existingCount, int totalCount)
    {
        string message = $"{existingCount} fichier(s) sur {totalCount} existe(nt) déjà.\n\n" +
                        "Voulez-vous :\n" +
                        "• [Oui] Écraser les fichiers existants\n" +
                        "• [Non] Ignorer et conserver les existants\n" +
                        "• [Annuler] Annuler l'opération";

        var result = MessageBox.Show(message, "Fichiers existants", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        
        return result switch
        {
            MessageBoxResult.Yes => OverwriteChoice.Overwrite,
            MessageBoxResult.No => OverwriteChoice.Skip,
            _ => OverwriteChoice.Cancel
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
