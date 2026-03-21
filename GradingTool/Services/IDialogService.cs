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
    void ShowToast(string message, int durationMs = 3000);
}
