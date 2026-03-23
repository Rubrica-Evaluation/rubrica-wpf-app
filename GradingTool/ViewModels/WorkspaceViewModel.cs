using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GradingTool.Helpers;
using GradingTool.Services;
using GradingTool.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace GradingTool.ViewModels;

public partial class WorkspaceViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly ISessionsRootService _sessionsRootService;
    private readonly ISessionService _sessionService;
    private readonly ICourseService _courseService;
    private readonly IWorkService _workService;
    private readonly IConfigurationService _configurationService;
    private readonly IRubricService _rubricService;
    private readonly IRosterService _rosterService;
    private readonly IGridService _gridService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string? _sessionsRootPath;

    [ObservableProperty]
    private bool _isSessionsRootConfigured;

    [ObservableProperty]
    private string _selectFolderButtonText = "Sélectionner dossier";

    [ObservableProperty]
    private ObservableCollection<string> _sessions = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditSessionCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSessionCommand))]
    [NotifyPropertyChangedFor(nameof(IsSessionSelected))]
    private string? _selectedSession;

    [ObservableProperty]
    private ObservableCollection<string> _courses = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCourseCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCourseCommand))]
    [NotifyPropertyChangedFor(nameof(IsCourseSelected))]
    private string? _selectedCourse;

    [ObservableProperty]
    private ObservableCollection<string> _works = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditWorkCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteWorkCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenRubricDesignerCommand))]
    private string? _selectedWork;

    [ObservableProperty]
    private Dictionary<string, string>? _workSubdirectories;

    [ObservableProperty]
    private bool _rubricExists;

    [ObservableProperty]
    private string _rubricStatusText = "Aucune rubrique trouvée";

    [ObservableProperty]
    private string _rubricStatusColor = "#DC3545";

    [ObservableProperty]
    private bool _rosterExists;

    [ObservableProperty]
    private string _rosterStatusText = "Aucune liste d'étudiants trouvée";

    [ObservableProperty]
    private string _rosterStatusColor = "#DC3545";

    [ObservableProperty]
    private ObservableCollection<GroupModel> _groups = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateGroupGridsCommand))]
    private GroupModel? _selectedGroup;

    partial void OnSelectedGroupChanged(GroupModel? value)
    {
        // Détecter si le groupe sélectionné contient des équipes
        HasTeams = value != null && value.Students.Any(s => s.Team > 0);
        
        // Réinitialiser le mode de génération à individual par défaut
        if (HasTeams)
        {
            GenerationMode = "individual";
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateGroupGridsCommand))]
    private bool _canGenerateGroupGrids;

    [ObservableProperty]
    private bool _hasTeams;

    [ObservableProperty]
    private string _generationMode = "individual"; // "individual" or "team"

    public bool IsSessionSelected => !string.IsNullOrEmpty(SelectedSession);

    public bool IsCourseSelected => !string.IsNullOrEmpty(SelectedCourse);

    public WorkspaceViewModel(
        IDialogService dialogService, 
        ISessionsRootService sessionsRootService,
        ISessionService sessionService,
        ICourseService courseService,
        IWorkService workService,
        IConfigurationService configurationService,
        IRubricService rubricService,
        IRosterService rosterService,
        IGridService gridService,
        INavigationService navigationService)
    {
        _dialogService = dialogService;
        _sessionsRootService = sessionsRootService;
        _sessionService = sessionService;
        _courseService = courseService;
        _workService = workService;
        _configurationService = configurationService;
        _rubricService = rubricService;
        _rosterService = rosterService;
        _gridService = gridService;
        _navigationService = navigationService;
        LoadSessionsRootPath();
    }

    private void LoadSessionsRootPath()
    {
        if (_sessionsRootService.IsConfigured())
        {
            SessionsRootPath = _sessionsRootService.GetSessionsRootPath();
            IsSessionsRootConfigured = true;
            SelectFolderButtonText = "Modifier dossier racine";
            LoadSessions();
        }
        else
        {
            // Essayer de détecter automatiquement le dossier sessions
            TryAutoDetectSessionsRoot();
            
            if (!IsSessionsRootConfigured)
            {
                IsSessionsRootConfigured = false;
                SelectFolderButtonText = "Sélectionner dossier racine";
            }
        }
    }

    private void TryAutoDetectSessionsRoot()
    {
        try
        {
            // Chercher dans le répertoire parent de l'application
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Directory.GetParent(appDirectory)?.Parent?.Parent?.Parent?.FullName;
            
            if (projectRoot != null)
            {
                var sessionsPath = Path.Combine(projectRoot, "sessions");
                if (Directory.Exists(sessionsPath))
                {
                    // Configurer automatiquement
                    _sessionsRootService.SetupSessionsRoot(sessionsPath, false);
                    SessionsRootPath = sessionsPath;
                    IsSessionsRootConfigured = true;
                    SelectFolderButtonText = "Modifier dossier racine";
                    LoadSessions();
                    _dialogService.ShowToast("Dossier racine détecté automatiquement");
                    return;
                }
            }
        }
        catch
        {
            // Ignorer les erreurs de détection automatique
        }
    }

    private void LoadSessions()
    {
        Sessions.Clear();
        var sessions = _sessionService.GetSessions();
        foreach (var session in sessions)
        {
            Sessions.Add(session);
        }
        
        // Restaurer la session sélectionnée précédemment
        var savedSession = _configurationService.LoadSelectedSession();
        if (!string.IsNullOrEmpty(savedSession) && Sessions.Contains(savedSession))
        {
            SelectedSession = savedSession;
        }
    }

    private void LoadCourses()
    {
        Courses.Clear();
        SelectedCourse = null;
        
        if (string.IsNullOrEmpty(SelectedSession))
            return;

        var courses = _courseService.GetCourses(SelectedSession);
        foreach (var course in courses)
        {
            Courses.Add(course);
        }
        
        // Restaurer le cours sélectionné précédemment
        var savedCourse = _configurationService.LoadSelectedCourse();
        if (!string.IsNullOrEmpty(savedCourse) && Courses.Contains(savedCourse))
        {
            SelectedCourse = savedCourse;
        }
    }

    partial void OnSelectedSessionChanged(string? value)
    {
        _configurationService.SaveSelectedSession(value);
        LoadCourses();
    }

    private void LoadWorks()
    {
        Works.Clear();
        SelectedWork = null;
        WorkSubdirectories = null;
        
        if (string.IsNullOrEmpty(SelectedSession) || string.IsNullOrEmpty(SelectedCourse))
            return;

        var works = _workService.GetWorks(SelectedSession, SelectedCourse);
        foreach (var work in works)
        {
            Works.Add(work);
        }
        
        // Restaurer le travail sélectionné précédemment
        var savedWork = _configurationService.LoadSelectedWork();
        if (!string.IsNullOrEmpty(savedWork) && Works.Contains(savedWork))
        {
            SelectedWork = savedWork;
        }
    }

    partial void OnSelectedCourseChanged(string? value)
    {
        _configurationService.SaveSelectedCourse(value);
        LoadWorks();
    }

    partial void OnSelectedWorkChanged(string? value)
    {
        _configurationService.SaveSelectedWork(value);
        
        if (string.IsNullOrEmpty(SelectedSession) || string.IsNullOrEmpty(SelectedCourse) || string.IsNullOrEmpty(value))
        {
            WorkSubdirectories = null;
            RubricExists = false;
            RubricStatusText = "Aucune rubrique trouvée";
            RubricStatusColor = "#DC3545";
            return;
        }

        // Vérifier la structure des sous-dossiers
        var missingDirectories = _workService.VerifyStructure(SelectedSession, SelectedCourse, value);
        
        if (missingDirectories.Count > 0)
        {
            var directoriesList = string.Join(", ", missingDirectories);
            var message = $"L'évaluation '{value}' ne contient pas tous les sous-dossiers requis.\n\n" +
                         $"Dossiers manquants: {directoriesList}\n\n" +
                         $"Voulez-vous créer les dossiers manquants?";
            
            if (_dialogService.ShowConfirmation(message, "Structure incomplète"))
            {
                try
                {
                    _workService.EnsureStructure(SelectedSession, SelectedCourse, value);
                    _dialogService.ShowToast($"Structure créée avec succès pour '{value}'");
                }
                catch (Exception ex)
                {
                    _dialogService.ShowMessage($"Erreur lors de la création de la structure: {ex.Message}", "Erreur", System.Windows.MessageBoxImage.Error);
                }
            }
        }

        WorkSubdirectories = _workService.GetWorkSubdirectories(SelectedSession, SelectedCourse, value);
        
        // Vérifier le statut de la rubrique
        UpdateRubricStatus();

        // Vérifier le statut du roster
        UpdateRosterStatus();
    }

    private void UpdateRubricStatus()
    {
        if (string.IsNullOrEmpty(SelectedSession) || string.IsNullOrEmpty(SelectedCourse) || string.IsNullOrEmpty(SelectedWork))
        {
            RubricExists = false;
            RubricStatusText = "Aucune rubrique trouvée";
            RubricStatusColor = "#DC3545";
            return;
        }

        var exists = _rubricService.RubricExists(SelectedSession, SelectedCourse, SelectedWork);
        RubricExists = exists;
        
        if (exists)
        {
            // Try to load and validate
            var rubric = _rubricService.LoadRubric(SelectedSession, SelectedCourse, SelectedWork, out string errorMessage);
            if (rubric != null)
            {
                RubricStatusText = "✓ Rubrique valide trouvée";
                RubricStatusColor = "#28A745";
            }
            else
            {
                RubricStatusText = $"✗ Rubrique invalide: {errorMessage}";
                RubricStatusColor = "#FFA500";
            }
        }
        else
        {
            RubricStatusText = "Aucune rubrique trouvée";
            RubricStatusColor = "#DC3545";
        }

        // Update group grid generation capability
        UpdateCanGenerateGroupGrids();
    }

    private void UpdateRosterStatus()
    {
        if (string.IsNullOrEmpty(SelectedSession) || string.IsNullOrEmpty(SelectedCourse) || string.IsNullOrEmpty(SelectedWork))
        {
            RosterExists = false;
            RosterStatusText = "Aucun roster trouvé";
            RosterStatusColor = "#DC3545";
            Groups.Clear();
            return;
        }

        var exists = _rosterService.RosterExists(SelectedSession, SelectedCourse, SelectedWork);
        RosterExists = exists;

        if (exists)
        {
            // Try to load and validate
            var roster = _rosterService.LoadRoster(SelectedSession, SelectedCourse, SelectedWork, out string errorMessage);
            if (roster != null)
            {
                RosterStatusText = "✓ Liste d'étudiants valide trouvée";
                RosterStatusColor = "#28A745";

                // Update groups list
                Groups.Clear();
                foreach (var group in roster.Groups)
                {
                    Groups.Add(group);
                }

                // Sélectionner le premier groupe automatiquement
                if (Groups.Count > 0)
                {
                    SelectedGroup = Groups[0];
                }

                if (roster.Groups.Count == 0)
                {
                    RosterStatusText = "✓ Liste d'étudiants trouvée (aucun groupe détecté)";
                    RosterStatusColor = "#FFA500";
                    SelectedGroup = null;
                }
            }
            else
            {
                RosterStatusText = $"✗ Liste d'étudiants invalide: {errorMessage}";
                RosterStatusColor = "#FFA500";
                Groups.Clear();
                SelectedGroup = null;
                HasTeams = false;
            }
        }
        else
        {
            RosterStatusText = "Aucune liste d'étudiants trouvée";
            RosterStatusColor = "#DC3545";
            Groups.Clear();
            SelectedGroup = null;
            HasTeams = false;
        }

        // Update test grid generation capability
        UpdateCanGenerateGroupGrids();
    }

    private void UpdateCanGenerateGroupGrids()
    {
        CanGenerateGroupGrids = RubricExists && RosterExists && SelectedGroup != null;
    }

    [RelayCommand]
    private void SelectSessionsRoot()
    {
        var selectedPath = _dialogService.SelectFolder("Sélectionner le dossier 'Evaluation-App'");
        if (selectedPath == null)
            return;

        // Vérifier si l'utilisateur a sélectionné un dossier nommé "Evaluation-App"
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
            
            SelectFolderButtonText = "Modifier dossier";
            SessionsRootPath = _sessionsRootService.GetSessionsRootPath();
            IsSessionsRootConfigured = true;

            WarnIfOneDriveInactive(SessionsRootPath);

            LoadSessions();

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

    private void WarnIfOneDriveInactive(string? path)
    {
        if (!OneDriveHelper.ShouldWarnUser(path))
            return;

        _dialogService.ShowMessage(
            "Le dossier sélectionné se trouve dans OneDrive, mais OneDrive n'est pas démarré.\n\n"
            + "Cela pourrait entraîner une perte de données ou des conflits de synchronisation.\n\n"
            + "Conseil : démarrez OneDrive avant de continuer à travailler.",
            "Avertissement — OneDrive inactif",
            System.Windows.MessageBoxImage.Warning);
    }

    [RelayCommand]
    private void CreateNewSession()
    {
        var sessionName = Views.InputDialog.Show(
            "Entrez le nom de la nouvelle session (ex: Hiver 2026):",
            "Nouvelle session");

        if (string.IsNullOrWhiteSpace(sessionName))
            return;

        try
        {
            _sessionService.CreateSession(sessionName);
            LoadSessions();
            SelectedSession = sessionName;
            
            _dialogService.ShowToast($"Session '{sessionName}' créée avec succès");
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                ex.Message,
                "Erreur",
                System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSession))]
    private void EditSession()
    {
        if (string.IsNullOrEmpty(SelectedSession))
            return;

        var newName = Views.InputDialog.Show(
            $"Modifier le nom de la session:",
            "Modifier session",
            SelectedSession);

        if (string.IsNullOrWhiteSpace(newName) || newName == SelectedSession)
            return;

        try
        {
            _sessionService.RenameSession(SelectedSession, newName);
            var oldSelection = SelectedSession;
            LoadSessions();
            SelectedSession = newName;

            _dialogService.ShowToast($"Session renommée avec succès");
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                ex.Message,
                "Erreur",
                System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSession))]
    private void DeleteSession()
    {
        if (string.IsNullOrEmpty(SelectedSession))
            return;

        // Vérifier si la session contient des sous-dossiers
        var hasSubdirectories = _sessionService.HasSubdirectories(SelectedSession);
        var message = hasSubdirectories
            ? $"La session '{SelectedSession}' contient des sous-dossiers.\n\nÊtes-vous sûr de vouloir la supprimer?"
            : $"Êtes-vous sûr de vouloir supprimer la session '{SelectedSession}'?";

        if (!_dialogService.ShowConfirmation(message, "Supprimer session"))
            return;

        try
        {
            _sessionService.DeleteSession(SelectedSession);
            SelectedSession = null;
            LoadSessions();

            _dialogService.ShowToast("Session supprimée avec succès");
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                ex.Message,
                "Erreur",
                System.Windows.MessageBoxImage.Error);
        }
    }

    private bool CanDeleteSession() => !string.IsNullOrEmpty(SelectedSession);

    [RelayCommand]
    private void CreateNewCourse()
    {
        if (string.IsNullOrEmpty(SelectedSession))
            return;

        var courseName = Views.InputDialog.Show(
            "Entrez le nom du nouveau cours (ex: BD1):",
            "Nouveau cours");

        if (string.IsNullOrWhiteSpace(courseName))
            return;

        try
        {
            _courseService.CreateCourse(SelectedSession, courseName);
            LoadCourses();
            SelectedCourse = courseName;

            _dialogService.ShowToast($"Cours '{courseName}' créé avec succès");
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                ex.Message,
                "Erreur",
                System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteCourse))]
    private void EditCourse()
    {
        if (string.IsNullOrEmpty(SelectedSession) || string.IsNullOrEmpty(SelectedCourse))
            return;

        var newName = Views.InputDialog.Show(
            $"Modifier le nom du cours:",
            "Modifier cours",
            SelectedCourse);

        if (string.IsNullOrWhiteSpace(newName) || newName == SelectedCourse)
            return;

        try
        {
            _courseService.RenameCourse(SelectedSession, SelectedCourse, newName);
            var oldSelection = SelectedCourse;
            LoadCourses();
            SelectedCourse = newName;

            _dialogService.ShowToast($"Cours renommé avec succès");
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                ex.Message,
                "Erreur",
                System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteCourse))]
    private void DeleteCourse()
    {
        if (string.IsNullOrEmpty(SelectedSession) || string.IsNullOrEmpty(SelectedCourse))
            return;

        var hasSubdirectories = _courseService.HasSubdirectories(SelectedSession, SelectedCourse);
        var message = hasSubdirectories
            ? $"Le cours '{SelectedCourse}' contient des sous-dossiers.\n\nÊtes-vous sûr de vouloir le supprimer?"
            : $"Êtes-vous sûr de vouloir supprimer le cours '{SelectedCourse}'?";

        if (!_dialogService.ShowConfirmation(message, "Supprimer cours"))
            return;

        try
        {
            _courseService.DeleteCourse(SelectedSession, SelectedCourse);
            SelectedCourse = null;
            LoadCourses();

            _dialogService.ShowToast("Cours supprimé avec succès");
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                ex.Message,
                "Erreur",
                System.Windows.MessageBoxImage.Error);
        }
    }

    private bool CanDeleteCourse() => !string.IsNullOrEmpty(SelectedCourse);

    [RelayCommand]
    private void CreateNewWork()
    {
        if (string.IsNullOrEmpty(SelectedSession) || string.IsNullOrEmpty(SelectedCourse))
            return;

        var workName = Views.InputDialog.Show(
            "Entrez le nom de la nouvelle évaluation (ex: TP1):",
            "Nouvelle évaluation");

        if (string.IsNullOrWhiteSpace(workName))
            return;

        try
        {
            _workService.CreateWork(SelectedSession, SelectedCourse, workName);
            LoadWorks();
            SelectedWork = workName;

            _dialogService.ShowToast($"Évaluation '{workName}' créée avec succès (rubric, roster, submissions, grading, pdf_docs)");
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                ex.Message,
                "Erreur",
                System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteWork))]
    private void EditWork()
    {
        if (string.IsNullOrEmpty(SelectedSession) || string.IsNullOrEmpty(SelectedCourse) || string.IsNullOrEmpty(SelectedWork))
            return;

        var newName = Views.InputDialog.Show(
            $"Modifier le nom de l'évaluation:",
            "Modifier évaluation",
            SelectedWork);

        if (string.IsNullOrWhiteSpace(newName) || newName == SelectedWork)
            return;

        try
        {
            _workService.RenameWork(SelectedSession, SelectedCourse, SelectedWork, newName);
            var oldSelection = SelectedWork;
            LoadWorks();
            SelectedWork = newName;

            _dialogService.ShowToast($"Évaluation renommée avec succès");
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                ex.Message,
                "Erreur",
                System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteWork))]
    private void DeleteWork()
    {
        if (string.IsNullOrEmpty(SelectedSession) || string.IsNullOrEmpty(SelectedCourse) || string.IsNullOrEmpty(SelectedWork))
            return;

        var message = $"Êtes-vous sûr de vouloir supprimer l'évaluation '{SelectedWork}'?";

        if (!_dialogService.ShowConfirmation(message, "Supprimer évaluation"))
            return;

        try
        {
            _workService.DeleteWork(SelectedSession, SelectedCourse, SelectedWork);
            SelectedWork = null;
            LoadWorks();

            _dialogService.ShowToast("Évaluation supprimée avec succès");
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                ex.Message,
                "Erreur",
                System.Windows.MessageBoxImage.Error);
        }
    }

    private bool CanDeleteWork() => !string.IsNullOrEmpty(SelectedWork);

    private bool CanOpenRubricDesigner() =>
        !string.IsNullOrEmpty(SelectedSession) &&
        !string.IsNullOrEmpty(SelectedCourse) &&
        !string.IsNullOrEmpty(SelectedWork);

    [RelayCommand(CanExecute = nameof(CanOpenRubricDesigner))]
    private void OpenRubricDesigner()
    {
        if (string.IsNullOrEmpty(SelectedSession) || string.IsNullOrEmpty(SelectedCourse) || string.IsNullOrEmpty(SelectedWork))
            return;

        _navigationService.NavigateTo<RubricDesignerViewModel>();

        if (_navigationService.CurrentView is RubricDesignerViewModel designer)
        {
            designer.Initialize(SelectedSession, SelectedCourse, SelectedWork);
        }
    }

    [RelayCommand]
    private void OpenGradingFolder()
    {
        if (string.IsNullOrEmpty(SelectedSession) || string.IsNullOrEmpty(SelectedCourse) || string.IsNullOrEmpty(SelectedWork) || SelectedGroup == null)
            return;

        try
        {
            var rootPath = _sessionsRootService.GetSessionsRootPath();
            if (string.IsNullOrEmpty(rootPath))
            {
                _dialogService.ShowMessage(
                    "Le dossier racine des sessions n'est pas configuré.",
                    "Erreur de configuration",
                    System.Windows.MessageBoxImage.Error);
                return;
            }

            var gradingPath = Path.Combine(rootPath, SelectedSession, SelectedCourse, SelectedWork, "grading", SelectedGroup.GroupCode);
            
            if (!Directory.Exists(gradingPath))
            {
                Directory.CreateDirectory(gradingPath);
            }

            Process.Start("explorer.exe", gradingPath);
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                $"Impossible d'ouvrir le dossier de grading:\n\n{ex.Message}",
                "Erreur",
                System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void NavigateToGridEditor()
    {
        if (SelectedGroup == null || string.IsNullOrEmpty(SelectedSession) || 
            string.IsNullOrEmpty(SelectedCourse) || string.IsNullOrEmpty(SelectedWork))
        {
            _dialogService.ShowMessage(
                "Veuillez sélectionner un groupe avant d'ouvrir l'éditeur.",
                "Groupe requis",
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        var rootPath = _sessionsRootService.GetSessionsRootPath();
        var gradingPath = Path.Combine(rootPath, SelectedSession, SelectedCourse, SelectedWork, "grading", SelectedGroup.GroupCode);

        _navigationService.NavigateTo<GridEditorViewModel>();
        
        // Initialiser l'éditeur avec les données du groupe
        if (_navigationService.CurrentView is GridEditorViewModel editor)
        {
            editor.Initialize(SelectedGroup, gradingPath, SelectedSession ?? "", SelectedCourse ?? "", SelectedWork ?? "");
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.NavigateBack();
    }

    [RelayCommand]
    private void LoadRoster()
    {
        if (string.IsNullOrEmpty(SelectedSession) || string.IsNullOrEmpty(SelectedCourse) || string.IsNullOrEmpty(SelectedWork))
            return;

        var selectedFiles = _dialogService.SelectFiles(
            "Sélectionner des fichiers CSV d'étudiants", 
            "Fichiers CSV|*.csv|Tous les fichiers|*.*");

        if (selectedFiles == null || selectedFiles.Length == 0)
            return;

        int successCount = 0;
        int skipCount = 0;
        var errorMessages = new List<string>();

        foreach (var selectedFile in selectedFiles)
        {
            var fileName = Path.GetFileName(selectedFile);

            // Vérifier si un fichier avec le même nom existe déjà
            if (_rosterService.FileExistsInRosterFolder(SelectedSession, SelectedCourse, SelectedWork, fileName))
            {
                if (!_dialogService.ShowConfirmation(
                    $"Un fichier '{fileName}' existe déjà pour cette évaluation.\n\nVoulez-vous le remplacer?",
                    "Fichier existant"))
                {
                    skipCount++;
                    continue;
                }
            }

            try
            {
                _rosterService.ImportRoster(SelectedSession, SelectedCourse, SelectedWork, selectedFile);
                successCount++;
            }
            catch (Exception ex)
            {
                errorMessages.Add($"{fileName}: {ex.Message}");
            }
        }

        // Mettre à jour l'interface une seule fois à la fin
        UpdateRosterStatus();

        // Afficher le résumé
        if (successCount > 0)
        {
            var message = $"{successCount} groupe(s) chargé(s) avec succès";
            if (skipCount > 0)
                message += $", {skipCount} ignoré(s)";
            if (errorMessages.Count > 0)
                message += $", {errorMessages.Count} erreur(s)";

            _dialogService.ShowToast(message);
        }

        // Afficher les erreurs détaillées si nécessaire
        if (errorMessages.Count > 0)
        {
            var errorSummary = string.Join("\n", errorMessages);
            _dialogService.ShowMessage(
                $"Erreurs lors du chargement:\n\n{errorSummary}",
                "Erreurs de chargement",
                System.Windows.MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void DownloadRosterTemplate()
    {
        var saveFile = _dialogService.SaveFile(
            "Enregistrer le template de roster",
            "roster_template.csv",
            "Fichiers CSV|*.csv");

        if (string.IsNullOrEmpty(saveFile))
            return;

        try
        {
            _rosterService.SaveRosterTemplate(saveFile);
            _dialogService.ShowToast($"Template enregistré: {Path.GetFileName(saveFile)}");
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                $"Erreur lors de la création du template:\n\n{ex.Message}",
                "Erreur",
                System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void DeleteRosterFile(GroupModel group)
    {
        if (group == null || string.IsNullOrEmpty(group.FilePath))
            return;

        var confirmed = _dialogService.ShowConfirmation(
            $"Voulez-vous vraiment supprimer la liste d'étudiants '{group.DisplayName}' ?\n\nLe fichier sera envoyé à la corbeille.",
            "Confirmer la suppression");

        if (!confirmed)
            return;

        try
        {
            _rosterService.DeleteRosterFile(group.FilePath);
            _dialogService.ShowToast($"Liste d'étudiants '{group.DisplayName}' supprimée");
            UpdateRosterStatus();
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                $"Erreur lors de la suppression de la liste d'étudiants:\n\n{ex.Message}",
                "Erreur",
                System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanGenerateGroupGrids))]
    private async Task GenerateGroupGrids()
    {
        if (string.IsNullOrEmpty(SelectedSession) || string.IsNullOrEmpty(SelectedCourse) || string.IsNullOrEmpty(SelectedWork) || SelectedGroup == null)
            return;

        try
        {
            // Charger le rubric
            var rubric = _rubricService.LoadRubric(SelectedSession, SelectedCourse, SelectedWork, out string rubricError);
            if (rubric == null)
            {
                _dialogService.ShowMessage(
                    $"Impossible de charger la rubrique: {rubricError}",
                    "Erreur de rubrique",
                    System.Windows.MessageBoxImage.Error);
                return;
            }

            // Chemin de base pour sauvegarder
            var rootPath = _sessionsRootService.GetSessionsRootPath();
            if (string.IsNullOrEmpty(rootPath))
            {
                _dialogService.ShowMessage(
                    "Le dossier racine des sessions n'est pas configuré.",
                    "Erreur de configuration",
                    System.Windows.MessageBoxImage.Error);
                return;
            }
            var basePath = Path.Combine(rootPath, SelectedSession, SelectedCourse, SelectedWork, "grading");

            // Vérifier le mode de génération (individuel ou équipe)
            if (GenerationMode == "team" && HasTeams)
            {
                await GenerateTeamGrids(rubric, basePath);
            }
            else
            {
                await GenerateIndividualGrids(rubric, basePath);
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                $"Erreur lors de la génération des grilles:\n\n{ex.Message}",
                "Erreur",
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task GenerateIndividualGrids(RubricModel rubric, string basePath)
    {
        if (SelectedGroup == null) return;

        // Vérifier s'il y a des grilles existantes
        int existingCount = 0;
        foreach (var student in SelectedGroup.Students)
        {
            if (_gridService.GridExists(student, basePath))
            {
                existingCount++;
            }
        }

        // Déterminer la stratégie (écraser, skip, ou annuler)
        bool shouldOverwrite = true;  // Par défaut, on écrase
        if (existingCount > 0)
        {
            var choice = _dialogService.ShowOverwriteConfirmation(existingCount, SelectedGroup.Students.Count);
            if (choice == OverwriteChoice.Cancel)
                return;
            
            shouldOverwrite = (choice == OverwriteChoice.Overwrite);
        }

        // Générer une grille pour chaque étudiant du groupe
        int generatedCount = 0;
        int skippedCount = 0;

        foreach (var student in SelectedGroup.Students)
        {
            // Vérifier si la grille existe déjà
            if (_gridService.GridExists(student, basePath))
            {
                if (!shouldOverwrite)
                {
                    skippedCount++;
                    continue;
                }
                // Sinon, on continue et on écrase
            }

            // Générer la grille
            var grid = _gridService.GenerateGrid(student, rubric, SelectedWork ?? "");

            // Sauvegarder la grille
            var success = await _gridService.SaveGridAsync(grid, basePath);
            if (success)
            {
                generatedCount++;
            }
            else
            {
                _dialogService.ShowMessage(
                    $"Erreur lors de la sauvegarde de la grille pour {student.FirstName} {student.LastName}.",
                    "Erreur de sauvegarde",
                    System.Windows.MessageBoxImage.Error);
                return; // Stop on first error
            }
        }

        if (generatedCount > 0)
        {
            _dialogService.ShowToast($"{generatedCount} grille(s) générée(s) pour le groupe {SelectedGroup.DisplayName} dans grading/{SelectedGroup.GroupCode}/");
        }
        
        if (skippedCount > 0)
        {
            _dialogService.ShowMessage(
                $"{skippedCount} grille(s) existaient déjà et ont été ignorées.",
                "Grilles ignorées",
                System.Windows.MessageBoxImage.Information);
        }

        if (generatedCount == 0 && skippedCount == 0)
        {
            _dialogService.ShowMessage(
                "Aucune grille n'a été générée.",
                "Information",
                System.Windows.MessageBoxImage.Information);
        }
    }

    private async Task GenerateTeamGrids(RubricModel rubric, string basePath)
    {
        if (SelectedGroup == null) return;

        // Regrouper les étudiants par équipe
        var teams = SelectedGroup.Students
            .Where(s => s.Team > 0)
            .GroupBy(s => s.Team)
            .OrderBy(g => g.Key)
            .ToList();

        if (teams.Count == 0)
        {
            _dialogService.ShowMessage(
                "Aucune équipe détectée. Tous les étudiants ont un numéro d'équipe à 0 ou vide.",
                "Pas d'équipes",
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        // Récupérer les étudiants sans équipe
        var studentsWithoutTeam = SelectedGroup.Students
            .Where(s => s.Team <= 0)
            .ToList();

        // Compter le total de grilles à générer
        int totalGridsToGenerate = teams.Count + studentsWithoutTeam.Count;

        // Vérifier s'il y a des grilles existantes
        int existingCount = 0;
        foreach (var team in teams)
        {
            var teamStudents = team.ToList();
            if (_gridService.TeamGridExists(teamStudents, basePath, team.Key))
            {
                existingCount++;
            }
        }
        foreach (var student in studentsWithoutTeam)
        {
            if (_gridService.GridExists(student, basePath))
            {
                existingCount++;
            }
        }

        // Déterminer la stratégie (écraser, skip, ou annuler)
        bool shouldOverwrite = true;  // Par défaut, on écrase
        if (existingCount > 0)
        {
            var choice = _dialogService.ShowOverwriteConfirmation(existingCount, totalGridsToGenerate);
            if (choice == OverwriteChoice.Cancel)
                return;
            
            shouldOverwrite = (choice == OverwriteChoice.Overwrite);
        }

        // Générer une grille pour chaque équipe
        int generatedCount = 0;
        int skippedCount = 0;

        foreach (var team in teams)
        {
            var teamStudents = team.ToList();
            int teamNumber = team.Key;

            // Vérifier si la grille existe déjà
            if (_gridService.TeamGridExists(teamStudents, basePath, teamNumber))
            {
                if (!shouldOverwrite)
                {
                    skippedCount++;
                    continue;
                }
                // Sinon, on continue et on écrase
            }

            // Générer la grille d'équipe
            var grid = _gridService.GenerateTeamGrid(teamStudents, rubric, SelectedWork ?? "", teamNumber);

            // Sauvegarder la grille d'équipe
            var success = await _gridService.SaveTeamGridAsync(grid, teamStudents, teamNumber, basePath);
            if (success)
            {
                generatedCount++;
            }
            else
            {
                _dialogService.ShowMessage(
                    $"Erreur lors de la sauvegarde de la grille pour l'équipe {teamNumber}.",
                    "Erreur de sauvegarde",
                    System.Windows.MessageBoxImage.Error);
                return; // Stop on first error
            }
        }

        // Générer les grilles individuelles pour les étudiants sans équipe
        foreach (var student in studentsWithoutTeam)
        {
            // Vérifier si la grille existe déjà
            if (_gridService.GridExists(student, basePath))
            {
                if (!shouldOverwrite)
                {
                    skippedCount++;
                    continue;
                }
                // Sinon, on continue et on écrase
            }

            // Générer la grille
            var grid = _gridService.GenerateGrid(student, rubric, SelectedWork ?? "");

            // Sauvegarder la grille
            var success = await _gridService.SaveGridAsync(grid, basePath);
            if (success)
            {
                generatedCount++;
            }
            else
            {
                _dialogService.ShowMessage(
                    $"Erreur lors de la sauvegarde de la grille pour {student.FirstName} {student.LastName}.",
                    "Erreur de sauvegarde",
                    System.Windows.MessageBoxImage.Error);
                return; // Stop on first error
            }
        }

        if (generatedCount > 0)
        {
            var message = $"{generatedCount} grille(s) générée(s)";
            if (teams.Count > 0)
                message += $" ({teams.Count} d'équipe";
            if (studentsWithoutTeam.Count > 0)
                message += $"{(teams.Count > 0 ? ", " : "")} {studentsWithoutTeam.Count} individuelle(s)";
            if (teams.Count > 0)
                message += ")";
            message += $" pour le groupe {SelectedGroup.DisplayName} dans grading/{SelectedGroup.GroupCode}/";
            
            _dialogService.ShowToast(message);
        }
        
        if (skippedCount > 0)
        {
            _dialogService.ShowMessage(
                $"{skippedCount} grille(s) existaient déjà et ont été ignorées.",
                "Grilles ignorées",
                System.Windows.MessageBoxImage.Information);
        }

        if (generatedCount == 0 && skippedCount == 0)
        {
            _dialogService.ShowMessage(
                "Aucune grille n'a été générée.",
                "Information",
                System.Windows.MessageBoxImage.Information);
        }
    }
}
