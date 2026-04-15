using System.Reflection;
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
    private int _selectedBackupIntervalMinutes;

    [ObservableProperty]
    private int _selectedBackupMaxCount;

    public static IReadOnlyList<int> BackupIntervalOptions { get; } = [5, 10, 15, 30];
    public static IReadOnlyList<int> BackupMaxCountOptions { get; } = [5, 10];

    [ObservableProperty]
    private ObservableCollection<BackupInfo> _availableBackups = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RestoreBackupCommand))]
    private BackupInfo? _selectedBackup;

    public string AppVersion { get; } = ResolveAppVersion();

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
        var selectedPath = _dialogService.SelectFolder(string.Format(_localizationService["Workspace_Dialog_SelectFolderPrompt"], ISessionsRootService.EvaluationAppFolderName));
        if (selectedPath == null)
            return;

        var needsEvaluationAppFolder = !selectedPath.EndsWith(ISessionsRootService.EvaluationAppFolderName, StringComparison.OrdinalIgnoreCase);

        if (needsEvaluationAppFolder)
        {
            if (!_dialogService.ShowConfirmation(
                string.Format(_localizationService["Workspace_Dialog_CreateAppFolderBody"], ISessionsRootService.EvaluationAppFolderName),
                string.Format(_localizationService["Workspace_Dialog_CreateAppFolderTitle"], ISessionsRootService.EvaluationAppFolderName)))
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

            _dialogService.ShowToast(_localizationService["Workspace_Toast_RootConfigured"]);
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                ex.Message,
                _localizationService["Common_Error"],
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
                _localizationService["Config_Dialog_DisableBackupBody"],
                _localizationService["Config_Dialog_DisableBackupTitle"]);

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

    partial void OnSelectedBackupIntervalMinutesChanged(int value)
    {
        _configurationService.SaveBackupIntervalMinutes(value);
        _backupService.UpdateTimerInterval(value);
    }

    partial void OnSelectedBackupMaxCountChanged(int value)
    {
        _configurationService.SaveBackupMaxCount(value);
    }

    [RelayCommand(CanExecute = nameof(CanRestoreBackup))]
    private async Task RestoreBackup()
    {
        if (SelectedBackup == null)
            return;

        var firstConfirmed = _dialogService.ShowConfirmation(
            _localizationService["Config_Dialog_RestoreBackupBody"],
            _localizationService["Config_Dialog_RestoreBackupTitle"]);

        if (!firstConfirmed)
            return;

        var secondConfirmed = _dialogService.ShowConfirmation(
            _localizationService["Config_Dialog_ConfirmRestoreBody"],
            _localizationService["Config_Dialog_ConfirmRestoreTitle"]);

        if (!secondConfirmed)
            return;

        var success = await _backupService.RestoreBackupAsync(SelectedBackup.FilePath);

        if (success)
        {
            _backupService.SuppressNextBackup();
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath != null)
                System.Diagnostics.Process.Start(exePath);
            Application.Current.Shutdown();
        }
        else
        {
            _dialogService.ShowMessage(
                _localizationService["Config_Error_RestoreBody"],
                _localizationService["Config_Error_RestoreTitle"],
                MessageBoxImage.Error);
        }
    }

    private bool CanRestoreBackup() => SelectedBackup != null;

    private void LoadBackupSettings()
    {
        _suppressBackupToggleDialog = true;
        IsBackupEnabled = _configurationService.LoadBackupEnabled();
        _suppressBackupToggleDialog = false;

        SelectedBackupIntervalMinutes = _configurationService.LoadBackupIntervalMinutes();
        SelectedBackupMaxCount = _configurationService.LoadBackupMaxCount();

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
            _localizationService["Config_Warning_OneDriveBody"],
            _localizationService["Workspace_Warning_OneDriveTitle"],
            System.Windows.MessageBoxImage.Warning);
    }

    private static string ResolveAppVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var plusIndex = informationalVersion.IndexOf('+');
            return plusIndex >= 0
                ? informationalVersion[..plusIndex]
                : informationalVersion;
        }

        return assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    }
}
