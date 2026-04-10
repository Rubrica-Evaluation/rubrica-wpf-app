using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GradingTool.Helpers;
using GradingTool.Models;
using GradingTool.Services;

namespace GradingTool.ViewModels;

public partial class ConfigurationViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly ISessionsRootService _sessionsRootService;
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _localizationService;
    private readonly IConfigurationService _configurationService;
    private readonly IBackupService _backupService;

    [ObservableProperty]
    private string? _sessionsRootPath;

    [ObservableProperty]
    private bool _isSessionsRootConfigured;

    [ObservableProperty]
    private string _selectFolderButtonText = string.Empty;

    [ObservableProperty]
    private string _selectedLanguage = "fr";

    [ObservableProperty]
    private bool _isBackupEnabled;

    private bool _suppressBackupToggleDialog;

    [ObservableProperty]
    private ObservableCollection<BackupInfo> _availableBackups = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RestoreBackupCommand))]
    private BackupInfo? _selectedBackup;

    public ConfigurationViewModel(
        IDialogService dialogService,
        ISessionsRootService sessionsRootService,
        INavigationService navigationService,
        ILocalizationService localizationService,
        IConfigurationService configurationService,
        IBackupService backupService)
    {
        _dialogService = dialogService;
        _sessionsRootService = sessionsRootService;
        _navigationService = navigationService;
        _localizationService = localizationService;
        _configurationService = configurationService;
        _backupService = backupService;

        _selectedLanguage = _localizationService.CurrentLanguage;
        _localizationService.LanguageChanged += OnLanguageChanged;

        LoadSessionsRootPath();
        LoadBackupSettings();
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        _localizationService.SetLanguage(value);
    }

    private void OnLanguageChanged()
    {
        RefreshButtonText();
    }

    private void LoadSessionsRootPath()
    {
        if (_sessionsRootService.IsConfigured())
        {
            SessionsRootPath = _sessionsRootService.GetSessionsRootPath();
            IsSessionsRootConfigured = true;
        }
        else
        {
            SessionsRootPath = null;
            IsSessionsRootConfigured = false;
        }

        RefreshButtonText();
    }

    private void RefreshButtonText()
    {
        SelectFolderButtonText = IsSessionsRootConfigured
            ? _localizationService["Config_ChangeFolder"]
            : _localizationService["Config_SelectFolder"];
    }

    [RelayCommand]
    private void SelectSessionsRoot()
    {
        var selectedPath = _dialogService.SelectFolder($"Sélectionner le dossier '{ISessionsRootService.EvaluationAppFolderName}'");
        if (selectedPath == null)
            return;

        var needsEvaluationAppFolder = !selectedPath.EndsWith(ISessionsRootService.EvaluationAppFolderName, StringComparison.OrdinalIgnoreCase);

        if (needsEvaluationAppFolder)
        {
            if (!_dialogService.ShowConfirmation(
                $"Voulez-vous créer un dossier '{ISessionsRootService.EvaluationAppFolderName}' dans le répertoire sélectionné?",
                $"Créer dossier {ISessionsRootService.EvaluationAppFolderName}"))
            {
                return;
            }
        }

        try
        {
            _sessionsRootService.SetupSessionsRoot(selectedPath, needsEvaluationAppFolder);

            SessionsRootPath = _sessionsRootService.GetSessionsRootPath();
            IsSessionsRootConfigured = true;
            RefreshButtonText();

            WarnIfOneDriveInactive(SessionsRootPath);

            _dialogService.ShowToast("Dossier racine configuré avec succès");
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                ex.Message,
                "Erreur",
                System.Windows.MessageBoxImage.Error);
        }
    }

    partial void OnIsBackupEnabledChanged(bool value)
    {
        if (_suppressBackupToggleDialog)
            return;

        if (!value)
        {
            var confirmed = _dialogService.ShowConfirmation(
                "Désactiver les sauvegardes automatiques réduit votre protection contre la perte de données.\n\nÊtes-vous sûr de vouloir désactiver cette fonctionnalité ?",
                "Désactiver les sauvegardes");

            if (!confirmed)
            {
                _suppressBackupToggleDialog = true;
                IsBackupEnabled = true;
                _suppressBackupToggleDialog = false;
                return;
            }
        }

        _configurationService.SaveBackupEnabled(value);
    }

    [RelayCommand(CanExecute = nameof(CanRestoreBackup))]
    private async Task RestoreBackup()
    {
        if (SelectedBackup == null)
            return;

        var firstConfirmed = _dialogService.ShowConfirmation(
            "Attention : cette opération va écraser et remplacer définitivement tout le contenu actuel de votre dossier Rubrica.\n\nL'état actuel ne sera pas récupérable.\n\nVoulez-vous continuer ?",
            "Restaurer une sauvegarde");

        if (!firstConfirmed)
            return;

        var secondConfirmed = _dialogService.ShowConfirmation(
            "DERNIÈRE CONFIRMATION : toutes vos données actuelles seront perdues de façon irréversible et remplacées par la sauvegarde sélectionnée.\n\nL'application redémarrera automatiquement.\n\nConfirmez-vous la restauration ?",
            "Confirmer la restauration");

        if (!secondConfirmed)
            return;

        var success = await _backupService.RestoreBackupAsync(SelectedBackup.FilePath);

        if (success)
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath != null)
                System.Diagnostics.Process.Start(exePath);
            Application.Current.Shutdown();
        }
        else
        {
            _dialogService.ShowMessage(
                "Une erreur est survenue lors de la restauration. Vos données n'ont pas été modifiées.",
                "Erreur de restauration",
                MessageBoxImage.Error);
        }
    }

    private bool CanRestoreBackup() => SelectedBackup != null;

    private void LoadBackupSettings()
    {
        _suppressBackupToggleDialog = true;
        IsBackupEnabled = _configurationService.LoadBackupEnabled();
        _suppressBackupToggleDialog = false;

        var backups = _backupService.GetAvailableBackups();
        AvailableBackups = new ObservableCollection<BackupInfo>(backups);
    }

    [RelayCommand]
    private void GoBack()
    {
        LoadSessionsRootPath();
        _navigationService.NavigateBack();
    }

    private void WarnIfOneDriveInactive(string? path)
    {
        if (!OneDriveHelper.ShouldWarnUser(path))
            return;

        _dialogService.ShowMessage(
            "Le dossier sélectionné se trouve dans OneDrive, mais OneDrive n'est pas démarré.\n\n"
            + "Cela pourrait entraîner une perte de données ou des conflits de synchronisation.\n\n"
            + "Conseil : fermez l'application, démarrez OneDrive, puis relancez.",
            "Avertissement — OneDrive inactif",
            System.Windows.MessageBoxImage.Warning);
    }
}