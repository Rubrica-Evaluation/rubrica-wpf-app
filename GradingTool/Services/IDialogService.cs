using GradingTool.Models;
using System.Windows;

namespace GradingTool.Services;

public enum OverwriteChoice
{
    Overwrite,
    Skip,
    Cancel
}

public enum UnsavedChangesChoice
{
    Save,
    Discard,
    Cancel
}

public interface IDialogService
{
    string? SelectFolder(string title);
    string? SelectFile(string title, string filter);
    string[]? SelectFiles(string title, string filter);
    string? SaveFile(string title, string defaultFileName, string filter);
    void ShowMessage(string message, string title, MessageBoxImage icon = MessageBoxImage.Information);
    bool ShowConfirmation(string message, string title);
    OverwriteChoice ShowOverwriteConfirmation(int existingCount, int totalCount);
    UnsavedChangesChoice ShowUnsavedChangesConfirmation(string context);

    /// <summary>
    /// Affiche un dialog pour choisir le groupe cible d'un import CSV.
    /// </summary>
    /// <returns>null = annulé, "" = nouveau groupe, groupCode = groupe existant ou groupe à créer</returns>
    string? ShowGroupImportTargetDialog(string fileName, List<GroupModel> existingGroups, string? suggestedGroupCode = null, string? suggestedDisplayName = null);

    void ShowToast(string message, int durationMs = 3000);
}
