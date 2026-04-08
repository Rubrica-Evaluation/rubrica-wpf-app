using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GradingTool.Models;
using GradingTool.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;

namespace GradingTool.ViewModels;

public partial class RosterEditorViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IRosterService _rosterService;
    private readonly IDialogService _dialogService;
    private readonly ILocalizationService _localizationService;
    private readonly IGridService _gridService;
    private readonly ISessionsRootService _sessionsRootService;

    private string _sessionName = string.Empty;
    private string _courseName = string.Empty;
    private string _workName = string.Empty;
    private bool _isDirty = false;

    [ObservableProperty]
    private ObservableCollection<GroupModel> _groups = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteGroupCommand))]
    private GroupModel? _selectedGroup;

    [ObservableProperty]
    private ObservableCollection<EditableStudentModel> _students = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteStudentCommand))]
    private EditableStudentModel? _selectedStudent;

    [ObservableProperty]
    private string _navigationPath = string.Empty;

    public RosterEditorViewModel(
        INavigationService navigationService,
        IRosterService rosterService,
        IDialogService dialogService,
        ILocalizationService localizationService,
        IGridService gridService,
        ISessionsRootService sessionsRootService)
    {
        _navigationService = navigationService;
        _rosterService = rosterService;
        _dialogService = dialogService;
        _localizationService = localizationService;
        _gridService = gridService;
        _sessionsRootService = sessionsRootService;
        _localizationService.LanguageChanged += RefreshNavigationPath;
    }

    public void Initialize(string session, string course, string work)
    {
        _sessionName = session;
        _courseName = course;
        _workName = work;
        RefreshNavigationPath();
        LoadGroups();
    }

    private void RefreshNavigationPath()
    {
        NavigationPath = $"{_sessionName} / {_courseName} / {_workName} / {_localizationService["RosterEditor_NavLabel"]}";
    }

    private void LoadGroups()
    {
        UnsubscribeAllStudents();
        Groups.Clear();
        Students.Clear();
        SelectedGroup = null;

        var roster = _rosterService.LoadRoster(_sessionName, _courseName, _workName, out _);
        foreach (var group in roster?.Groups ?? [])
        {
            Groups.Add(group);
        }

        if (Groups.Count > 0)
        {
            SelectedGroup = Groups[0];
        }

        _isDirty = false;
    }

    private void UnsubscribeAllStudents()
    {
        foreach (var student in Students)
            student.PropertyChanged -= OnStudentPropertyChanged;
    }

    private void OnStudentPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => _isDirty = true;

    partial void OnSelectedGroupChanged(GroupModel? oldValue, GroupModel? newValue)
    {
        if (oldValue != null)
            CommitStudentsToGroup(oldValue);

        UnsubscribeAllStudents();
        Students.Clear();

        if (newValue == null)
            return;

        foreach (var student in newValue.Students)
            Students.Add(EditableStudentModel.FromStudent(student));

        foreach (var student in Students)
            student.PropertyChanged += OnStudentPropertyChanged;
    }

    private void CommitStudentsToGroup(GroupModel group)
    {
        group.Students.Clear();
        foreach (var editable in Students)
        {
            group.Students.Add(editable.ToStudent());
        }
    }

    [RelayCommand]
    private void AddGroup()
    {
        var groupCode = GenerateNextGroupCode();
        var displayName = $"Gr. {ExtractGroupNumber(groupCode)} ({groupCode})";
        var group = new GroupModel { GroupCode = groupCode, DisplayName = displayName };
        Groups.Add(group);
        SelectedGroup = group;
        _isDirty = true;
    }

    private string GenerateNextGroupCode()
    {
        int maxNumber = 0;
        foreach (var group in Groups)
        {
            var match = Regex.Match(group.GroupCode, @"gr(\d{5})");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int n))
            {
                maxNumber = Math.Max(maxNumber, n);
            }
        }

        return $"gr{(maxNumber + 1):D5}";
    }

    private static int ExtractGroupNumber(string groupCode)
    {
        var match = Regex.Match(groupCode, @"gr(\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out int n) ? n : 1;
    }

    private bool CanDeleteGroup() => SelectedGroup != null;

    [RelayCommand(CanExecute = nameof(CanDeleteGroup))]
    private void DeleteGroup()
    {
        if (SelectedGroup == null)
            return;

        var groupToDelete = SelectedGroup;
        var gradingBasePath = BuildGradingBasePath();
        var hasGradingFiles = gradingBasePath != null
            && _gridService.GradingFolderHasFiles(gradingBasePath, groupToDelete.GroupCode);

        var warning = hasGradingFiles
            ? $"\n\n\u26a0\ufe0f Des grilles de correction existent pour ce groupe (grading/{groupToDelete.GroupCode}/). Elles seront inaccessibles si le groupe est supprimé."
            : string.Empty;

        var confirmed = _dialogService.ShowConfirmation(
            $"Voulez-vous vraiment supprimer le groupe '{groupToDelete.DisplayName}' et ses {groupToDelete.Students.Count} étudiant(s) ?{warning}",
            "Confirmer la suppression");

        if (!confirmed)
            return;

        bool deleteGradingFiles = false;
        if (hasGradingFiles && gradingBasePath != null)
        {
            deleteGradingFiles = _dialogService.ShowConfirmation(
                $"Voulez-vous aussi supprimer les fichiers de grilles (grading/{groupToDelete.GroupCode}/) ?\n\nIls seront envoyés à la corbeille.",
                "Supprimer les grilles ?");
        }

        var index = Groups.IndexOf(groupToDelete);
        SelectedGroup = Groups.Count > 1 ? Groups[index == 0 ? 1 : index - 1] : null;
        Groups.Remove(groupToDelete);
        PersistGroups();

        if (deleteGradingFiles && gradingBasePath != null)
            _gridService.DeleteGradingFolder(gradingBasePath, groupToDelete.GroupCode);
    }

    private string? BuildGradingBasePath()
    {
        var root = _sessionsRootService.GetSessionsRootPath();
        if (string.IsNullOrEmpty(root))
            return null;
        return Path.Combine(root, _sessionName, _courseName, _workName, "grading");
    }

    [RelayCommand]
    private void AddStudent()
    {
        if (SelectedGroup == null)
            return;

        var student = new EditableStudentModel();
        student.PropertyChanged += OnStudentPropertyChanged;
        Students.Add(student);
        SelectedStudent = Students[^1];
        _isDirty = true;
    }

    private bool CanDeleteStudent() => SelectedStudent != null;

    [RelayCommand(CanExecute = nameof(CanDeleteStudent))]
    private void DeleteStudent()
    {
        if (SelectedStudent == null)
            return;

        SelectedStudent.PropertyChanged -= OnStudentPropertyChanged;
        Students.Remove(SelectedStudent);
        SelectedStudent = Students.Count > 0 ? Students[^1] : null;
        _isDirty = true;
    }

    private bool PersistGroups()
    {
        var duplicates = FindDuplicateDas();
        if (duplicates.Count > 0)
        {
            _dialogService.ShowMessage(
                $"Codes en double détectés :\n\n{string.Join("\n", duplicates)}\n\nCorrigez avant de sauvegarder.",
                "Codes en double",
                System.Windows.MessageBoxImage.Warning);
            return false;
        }

        try
        {
            _rosterService.SaveRoster(_sessionName, _courseName, _workName, Groups.ToList());
            _isDirty = false;
            return true;
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                $"Erreur lors de la sauvegarde:\n\n{ex.Message}",
                "Erreur",
                System.Windows.MessageBoxImage.Error);
            return false;
        }
    }

    private List<string> FindDuplicateDas() =>
        Groups
            .SelectMany(g => g.Students
                .Select(s => s.Da)
                .Where(da => !string.IsNullOrWhiteSpace(da))
                .GroupBy(da => da, StringComparer.OrdinalIgnoreCase)
                .Where(grp => grp.Count() > 1)
                .Select(grp => grp.Key))
            .ToList();

    public bool CanProceedWithUnsavedChanges()
    {
        if (!_isDirty) return true;

        var choice = _dialogService.ShowUnsavedChangesConfirmation("l'éditeur de groupes");

        if (choice == UnsavedChangesChoice.Discard) return true;
        if (choice == UnsavedChangesChoice.Cancel) return false;

        if (SelectedGroup != null)
            CommitStudentsToGroup(SelectedGroup);
        PersistGroups();
        return true;
    }

    [RelayCommand]
    private void Save()
    {
        if (SelectedGroup != null)
            CommitStudentsToGroup(SelectedGroup);

        if (PersistGroups())
            _dialogService.ShowToast(_localizationService["RosterEditor_SavedSuccess"]);
    }

    [RelayCommand]
    private void ImportCsv()
    {
        if (string.IsNullOrEmpty(_sessionName))
            return;

        var selectedFiles = _dialogService.SelectFiles(
            "Sélectionner des fichiers CSV d'étudiants",
            "Fichiers CSV|*.csv|Tous les fichiers|*.*");

        if (selectedFiles == null || selectedFiles.Length == 0)
            return;

        int successCount = 0;
        var errorMessages = new List<string>();

        foreach (var file in selectedFiles)
        {
            var fileName = System.IO.Path.GetFileName(file);
            var suggested = _rosterService.DetectGroupCodeInFileName(fileName);
            var suggestedDisplay = string.IsNullOrEmpty(suggested) ? null : _rosterService.BuildGroupDisplayName(suggested);
            var targetGroupCode = _dialogService.ShowGroupImportTargetDialog(fileName, Groups.ToList(), suggested, suggestedDisplay);
            if (targetGroupCode == null)
                continue; // annulé pour ce fichier

            try
            {
                _rosterService.ImportCsv(_sessionName, _courseName, _workName, file, targetGroupCode);
                successCount++;
            }
            catch (Exception ex)
            {
                errorMessages.Add($"{fileName}: {ex.Message}");
            }
        }

        if (errorMessages.Count > 0)
        {
            _dialogService.ShowMessage(
                $"Erreurs:\n\n{string.Join("\n", errorMessages)}",
                "Erreurs d'importation",
                System.Windows.MessageBoxImage.Warning);
        }

        if (successCount > 0)
        {
            _dialogService.ShowToast($"{successCount} groupe(s) importé(s)");
            LoadGroups();
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        if (!CanProceedWithUnsavedChanges())
            return;
        _navigationService.NavigateBack();
    }
}
