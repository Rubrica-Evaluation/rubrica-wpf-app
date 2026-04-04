using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GradingTool.Helpers;
using GradingTool.Services;

namespace GradingTool.ViewModels;

public partial class ConfigurationViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly ISessionsRootService _sessionsRootService;
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private string? _sessionsRootPath;

    [ObservableProperty]
    private bool _isSessionsRootConfigured;

    [ObservableProperty]
    private string _selectFolderButtonText = string.Empty;

    [ObservableProperty]
    private string _selectedLanguage = "fr";

    public ConfigurationViewModel(
        IDialogService dialogService,
        ISessionsRootService sessionsRootService,
        INavigationService navigationService,
        ILocalizationService localizationService)
    {
        _dialogService = dialogService;
        _sessionsRootService = sessionsRootService;
        _navigationService = navigationService;
        _localizationService = localizationService;

        _selectedLanguage = _localizationService.CurrentLanguage;
        _localizationService.LanguageChanged += OnLanguageChanged;

        LoadSessionsRootPath();
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
        var selectedPath = _dialogService.SelectFolder("Sélectionner le dossier 'Evaluation-App'");
        if (selectedPath == null)
            return;

        var needsEvaluationAppFolder = !selectedPath.EndsWith("Evaluation-App", StringComparison.OrdinalIgnoreCase);

        if (needsEvaluationAppFolder)
        {
            if (!_dialogService.ShowConfirmation(
                "Voulez-vous créer un dossier 'Evaluation-App' dans le répertoire sélectionné?",
                "Créer dossier Evaluation-App"))
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