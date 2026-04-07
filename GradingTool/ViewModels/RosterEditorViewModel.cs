using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GradingTool.Models;
using GradingTool.Services;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace GradingTool.ViewModels;

public partial class RosterEditorViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IRosterService _rosterService;
    private readonly IDialogService _dialogService;
    private readonly ILocalizationService _localizationService;

    private string _sessionName = string.Empty;
    private string _courseName = string.Empty;
    private string _workName = string.Empty;

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
        ILocalizationService localizationService)
    {
        _navigationService = navigationService;
        _rosterService = rosterService;
        _dialogService = dialogService;
        _localizationService = localizationService;
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
    }

    partial void OnSelectedGroupChanged(GroupModel? oldValue, GroupModel? newValue)
    {
        if (oldValue != null)
        {
            CommitStudentsToGroup(oldValue);
        }

        Students.Clear();

        if (newValue == null)
            return;

        foreach (var student in newValue.Students)
        {
            Students.Add(EditableStudentModel.FromStudent(student));
        }
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

        var confirmed = _dialogService.ShowConfirmation(
            $"Voulez-vous vraiment supprimer le groupe '{SelectedGroup.DisplayName}' et ses {SelectedGroup.Students.Count} étudiant(s) ?",
            "Confirmer la suppression");

        if (!confirmed)
            return;

        var groupToDelete = SelectedGroup;

        var index = Groups.IndexOf(groupToDelete);
        SelectedGroup = Groups.Count > 1 ? Groups[index == 0 ? 1 : index - 1] : null;
        Groups.Remove(groupToDelete);

        PersistGroups();
    }

    [RelayCommand]
    private void AddStudent()
    {
        if (SelectedGroup == null)
            return;

        Students.Add(new EditableStudentModel());
        SelectedStudent = Students[^1];
    }

    private bool CanDeleteStudent() => SelectedStudent != null;

    [RelayCommand(CanExecute = nameof(CanDeleteStudent))]
    private void DeleteStudent()
    {
        if (SelectedStudent == null)
            return;

        Students.Remove(SelectedStudent);
        SelectedStudent = Students.Count > 0 ? Students[^1] : null;
    }

    private void PersistGroups()
    {
        try
        {
            _rosterService.SaveRoster(_sessionName, _courseName, _workName, Groups.ToList());
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage(
                $"Erreur lors de la sauvegarde:\n\n{ex.Message}",
                "Erreur",
                System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (SelectedGroup != null)
        {
            CommitStudentsToGroup(SelectedGroup);
        }

        PersistGroups();
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
            try
            {
                _rosterService.ImportCsv(_sessionName, _courseName, _workName, file);
                successCount++;
            }
            catch (Exception ex)
            {
                errorMessages.Add($"{System.IO.Path.GetFileName(file)}: {ex.Message}");
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
        _navigationService.NavigateBack();
    }
}
