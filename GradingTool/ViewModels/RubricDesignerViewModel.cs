using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GradingTool.Models;
using GradingTool.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace GradingTool.ViewModels;

public partial class RubricDesignerViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IRubricService _rubricService;
    private readonly IDialogService _dialogService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private string _sessionName = string.Empty;

    [ObservableProperty]
    private string _courseName = string.Empty;

    [ObservableProperty]
    private string _workName = string.Empty;

    [ObservableProperty]
    private string _tpName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<object> _criteria = new();

    [ObservableProperty]
    private ObservableCollection<object> _penalties = new();

    [ObservableProperty]
    private bool _hasExistingRubric;

    private bool _isDirty;

    public bool HasUnsavedChanges => _isDirty;

    public string NavigationPath => $"{SessionName} / {CourseName} / {WorkName} / {_localizationService["RubricDesigner_NavLabel"]}";

    public int CriteriaCount => Criteria.Count;

    public int PenaltiesCount => Penalties.Count;

    public int TotalWeight => Criteria.OfType<RubricCriterionEditorModel>().Sum(criterion => criterion.Weight);

    public string CriteriaCountText => string.Format(_localizationService["RubricDesigner_CriteriaCountFmt"], CriteriaCount);

    public string PenaltiesCountText => string.Format(_localizationService["RubricDesigner_PenaltiesCountFmt"], PenaltiesCount);

    public string TotalWeightText => string.Format(_localizationService["RubricDesigner_TotalWeightFmt"], TotalWeight);

    public RubricDesignerViewModel(
        INavigationService navigationService,
        IRubricService rubricService,
        IDialogService dialogService,
        ILocalizationService localizationService)
    {
        _navigationService = navigationService;
        _rubricService = rubricService;
        _dialogService = dialogService;
        _localizationService = localizationService;
        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(NavigationPath));
        RefreshSummaryProperties();
    }

    public void Initialize(string sessionName, string courseName, string workName)
    {
        SessionName = sessionName;
        CourseName = courseName;
        WorkName = workName;
        OnPropertyChanged(nameof(NavigationPath));
        LoadDesignerState();
    }

    private void LoadDesignerState()
    {
        HasExistingRubric = _rubricService.RubricExists(SessionName, CourseName, WorkName);

        if (!HasExistingRubric)
        {
            LoadRubricIntoDesigner(_rubricService.CreateEmptyRubric(WorkName));
            return;
        }

        var rubric = _rubricService.LoadRubric(SessionName, CourseName, WorkName, out var errorMessage);
        if (rubric == null)
        {
            LoadRubricIntoDesigner(_rubricService.CreateEmptyRubric(WorkName));
            _dialogService.ShowMessage(
                $"La rubrique actuelle n'a pas pu être chargée dans le concepteur.\n\n{errorMessage}",
                "Rubrique invalide",
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        LoadRubricIntoDesigner(rubric);
    }

    private void LoadRubricIntoDesigner(RubricModel rubric)
    {
        DetachCollectionHandlers();

        TpName = string.IsNullOrWhiteSpace(rubric.Meta.Tp) ? WorkName : rubric.Meta.Tp;

        Criteria = new ObservableCollection<object>(
            rubric.Criteria.Select(CreateCriterionEditor).Cast<object>());

        Penalties = new ObservableCollection<object>(
            rubric.Penalties.Select(CreatePenaltyEditor).Cast<object>());

        AttachCollectionHandlers();
        RefreshSummaryProperties();
        HasExistingRubric = _rubricService.RubricExists(SessionName, CourseName, WorkName);
        _isDirty = false;
    }

    private void AttachCollectionHandlers()
    {
        Criteria.CollectionChanged += OnCriteriaCollectionChanged;
        Penalties.CollectionChanged += OnPenaltiesCollectionChanged;

        foreach (var criterion in Criteria.OfType<RubricCriterionEditorModel>())
            AttachCriterionHandlers(criterion);

        foreach (var penalty in Penalties.OfType<RubricPenaltyEditorModel>())
            penalty.PropertyChanged += OnAnyItemPropertyChanged;
    }

    private void DetachCollectionHandlers()
    {
        Criteria.CollectionChanged -= OnCriteriaCollectionChanged;
        Penalties.CollectionChanged -= OnPenaltiesCollectionChanged;

        foreach (var criterion in Criteria.OfType<RubricCriterionEditorModel>())
            DetachCriterionHandlers(criterion);

        foreach (var penalty in Penalties.OfType<RubricPenaltyEditorModel>())
            penalty.PropertyChanged -= OnAnyItemPropertyChanged;
    }

    private void OnCriteriaCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<RubricCriterionEditorModel>())
                DetachCriterionHandlers(item);
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<RubricCriterionEditorModel>())
                AttachCriterionHandlers(item);
        }

        _isDirty = true;
        RefreshSummaryProperties();
    }

    private void OnPenaltiesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<RubricPenaltyEditorModel>())
                item.PropertyChanged -= OnAnyItemPropertyChanged;
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<RubricPenaltyEditorModel>())
                item.PropertyChanged += OnAnyItemPropertyChanged;
        }

        _isDirty = true;
        RefreshSummaryProperties();
    }

    private void AttachCriterionHandlers(RubricCriterionEditorModel criterion)
    {
        criterion.PropertyChanged += OnCriterionPropertyChanged;
        criterion.Scale.CollectionChanged += OnCriterionScaleCollectionChanged;

        foreach (var scaleItem in criterion.Scale)
            scaleItem.PropertyChanged += OnAnyItemPropertyChanged;
    }

    private void DetachCriterionHandlers(RubricCriterionEditorModel criterion)
    {
        criterion.PropertyChanged -= OnCriterionPropertyChanged;
        criterion.Scale.CollectionChanged -= OnCriterionScaleCollectionChanged;

        foreach (var scaleItem in criterion.Scale)
            scaleItem.PropertyChanged -= OnAnyItemPropertyChanged;
    }

    private void OnCriterionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _isDirty = true;

        if (e.PropertyName == nameof(RubricCriterionEditorModel.Weight))
            RefreshSummaryProperties();
    }

    private void OnCriterionScaleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<RubricScaleItemEditorModel>())
                item.PropertyChanged -= OnAnyItemPropertyChanged;
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<RubricScaleItemEditorModel>())
                item.PropertyChanged += OnAnyItemPropertyChanged;
        }

        _isDirty = true;
        RefreshSummaryProperties();
    }

    private void OnAnyItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _isDirty = true;
    }

    private void RefreshSummaryProperties()
    {
        OnPropertyChanged(nameof(CriteriaCount));
        OnPropertyChanged(nameof(PenaltiesCount));
        OnPropertyChanged(nameof(TotalWeight));
        OnPropertyChanged(nameof(CriteriaCountText));
        OnPropertyChanged(nameof(PenaltiesCountText));
        OnPropertyChanged(nameof(TotalWeightText));
    }

    private static RubricCriterionEditorModel CreateCriterionEditor(CriterionModel criterion)
    {
        return new RubricCriterionEditorModel
        {
            Label = criterion.Label,
            Weight = criterion.Weight,
            Scale = new ObservableCollection<RubricScaleItemEditorModel>(
                criterion.Scale.Select(scale => new RubricScaleItemEditorModel
                {
                    Qualitative = scale.Qualitative,
                    Label = scale.Label,
                    Points = scale.Points
                }))
        };
    }

    private static RubricPenaltyEditorModel CreatePenaltyEditor(PenaltyItemModel penalty)
    {
        return new RubricPenaltyEditorModel
        {
            Label = penalty.Label,
            Factor = penalty.Factor,
            Min = penalty.Min
        };
    }

    private static RubricCriterionEditorModel CreateDefaultCriterion(int criterionNumber)
    {
        return new RubricCriterionEditorModel
        {
            Label = $"Critère {criterionNumber}",
            Weight = 0,
            Scale = new ObservableCollection<RubricScaleItemEditorModel>
            {
                new() { Qualitative = "Excellent", Label = "[Descripteur du niveau: Excellent]", Points = 100 },
                new() { Qualitative = "Très bien", Label = "[Descripteur du niveau: Très bien]", Points = 80 },
                new() { Qualitative = "Suffisant", Label = "[Descripteur du niveau: Suffisant]", Points = 60 },
                new() { Qualitative = "Insuffisant", Label = "[Descripteur du niveau: Insuffisant]", Points = 40 },
                new() { Qualitative = "Invalide", Label = "[Descripteur du niveau: Invalide]", Points = 0 }
            }
        };
    }

    private static RubricPenaltyEditorModel CreateDefaultPenalty(int penaltyNumber)
    {
        return new RubricPenaltyEditorModel
        {
            Label = $"Pénalité {penaltyNumber} (ex.: Nombre de jours de retard)",
            Factor = -10,
            Min = -30
        };
    }

    private RubricModel BuildRubric()
    {
        return new RubricModel
        {
            Meta = new RubricMeta
            {
                Tp = string.IsNullOrWhiteSpace(TpName) ? WorkName : TpName.Trim(),
                Student = new StudentModel
                {
                    Da = string.Empty,
                    FirstName = string.Empty,
                    LastName = string.Empty,
                    Group = string.Empty,
                    GroupCode = string.Empty,
                    Team = 0
                }
            },
            Criteria = Criteria.OfType<RubricCriterionEditorModel>().Select(criterion => new CriterionModel
            {
                Label = criterion.Label.Trim(),
                Weight = criterion.Weight,
                Result = string.Empty,
                Feedback = new ObservableCollection<CommentEntry>(),
                Points = null,
                Scale = criterion.Scale.Select(scale => new ScaleItemModel
                {
                    Qualitative = scale.Qualitative.Trim(),
                    Label = scale.Label.Trim(),
                    Points = scale.Points
                }).ToList()
            }).ToList(),
            Penalties = Penalties.OfType<RubricPenaltyEditorModel>().Select(penalty => new PenaltyItemModel
            {
                Label = penalty.Label.Trim(),
                Count = 0,
                Factor = penalty.Factor,
                Reason = string.Empty,
                Min = penalty.Min
            }).ToList(),
            Computed = new ComputedModel
            {
                Total = null
            }
        };
    }

    private bool ValidateDesignerState(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(TpName))
        {
            errorMessage = "Le nom du travail est requis.";
            return false;
        }

        if (Criteria.Count == 0)
        {
            errorMessage = "Ajoutez au moins un critère avant d'enregistrer la rubrique.";
            return false;
        }

        foreach (var criterion in Criteria.OfType<RubricCriterionEditorModel>())
        {
            if (string.IsNullOrWhiteSpace(criterion.Label))
            {
                errorMessage = "Chaque critère doit avoir un libellé.";
                return false;
            }

            if (criterion.Weight <= 0)
            {
                errorMessage = $"Le critère '{criterion.Label}' doit avoir un poids supérieur à 0.";
                return false;
            }

            if (criterion.Scale.Count == 0)
            {
                errorMessage = $"Le critère '{criterion.Label}' doit contenir au moins un niveau qualitatif.";
                return false;
            }

            if (criterion.Scale.Any(scale => string.IsNullOrWhiteSpace(scale.Qualitative) || string.IsNullOrWhiteSpace(scale.Label)))
            {
                errorMessage = $"Chaque niveau du critère '{criterion.Label}' doit avoir un code qualitatif et un libellé.";
                return false;
            }
        }

        if (Penalties.OfType<RubricPenaltyEditorModel>().Any(penalty => string.IsNullOrWhiteSpace(penalty.Label)))
        {
            errorMessage = "Chaque pénalité doit avoir un libellé.";
            return false;
        }

        return true;
    }

    [RelayCommand]
    private void LoadRubricFromFile()
    {
        var filePath = _dialogService.SelectFile(
            "Charger une rubrique",
            "Fichiers JSON|*.json");

        if (string.IsNullOrEmpty(filePath))
            return;

        var rubric = _rubricService.LoadRubricFromFile(filePath, out var errorMessage);
        if (rubric == null)
        {
            _dialogService.ShowMessage(
                errorMessage,
                "Fichier invalide",
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        if ((Criteria.Count > 0 || Penalties.Count > 0) &&
            !_dialogService.ShowConfirmation(
                "Remplacer le contenu actuel du concepteur par la rubrique chargée ?",
                "Charger une rubrique"))
        {
            return;
        }

        LoadRubricIntoDesigner(rubric);
        _dialogService.ShowToast("Rubrique chargée dans le concepteur");
    }

    [RelayCommand]
    private void NewRubric()
    {
        if ((Criteria.Count > 0 || Penalties.Count > 0) &&
            !_dialogService.ShowConfirmation(
                "Réinitialiser le concepteur et repartir d'une rubrique vide ?",
                "Nouvelle rubrique"))
        {
            return;
        }

        LoadRubricIntoDesigner(_rubricService.CreateEmptyRubric(WorkName));
    }

    [RelayCommand]
    private void AddCriterion()
    {
        Criteria.Add(CreateDefaultCriterion(Criteria.Count + 1));
    }

    [RelayCommand]
    private void RemoveCriterion(object? criterionItem)
    {
        if (criterionItem is not RubricCriterionEditorModel criterion)
            return;

        Criteria.Remove(criterion);
    }

    [RelayCommand]
    private void AddScaleItem(object? criterionItem)
    {
        if (criterionItem is not RubricCriterionEditorModel criterion)
            return;

        criterion.Scale.Add(new RubricScaleItemEditorModel
        {
            Qualitative = string.Empty,
            Label = string.Empty,
            Points = 0
        });
    }

    [RelayCommand]
    private void RemoveScaleItem(object? scaleItemValue)
    {
        if (scaleItemValue is not RubricScaleItemEditorModel scaleItem)
            return;

        var parentCriterion = Criteria
            .OfType<RubricCriterionEditorModel>()
            .FirstOrDefault(criterion => criterion.Scale.Contains(scaleItem));
        parentCriterion?.Scale.Remove(scaleItem);
    }

    [RelayCommand]
    private void AddPenalty()
    {
        Penalties.Add(CreateDefaultPenalty(Penalties.Count + 1));
    }

    [RelayCommand]
    private void RemovePenalty(object? penaltyItem)
    {
        if (penaltyItem is not RubricPenaltyEditorModel penalty)
            return;

        Penalties.Remove(penalty);
    }

    [RelayCommand]
    private void Save()
    {
        if (!ValidateDesignerState(out var errorMessage))
        {
            _dialogService.ShowMessage(errorMessage, "Rubrique incomplète", System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (TotalWeight != 100 && !_dialogService.ShowConfirmation(
                $"Le total des poids est de {TotalWeight} %. Voulez-vous enregistrer quand même ?",
                "Poids non équilibrés"))
        {
            return;
        }

        var rubric = BuildRubric();
        if (!_rubricService.SaveRubric(SessionName, CourseName, WorkName, rubric, out var saveError))
        {
            _dialogService.ShowMessage(
                $"La rubrique n'a pas pu être enregistrée.\n\n{saveError}",
                "Erreur de sauvegarde",
                System.Windows.MessageBoxImage.Error);
            return;
        }

        HasExistingRubric = true;
        _isDirty = false;
        _dialogService.ShowToast("Rubrique enregistrée avec succès");
    }

    /// <summary>
    /// Vérifie s'il y a des modifications non enregistrées et propose de sauvegarder.
    /// Retourne true si on peut continuer (enregistré ou abandonné), false si annulé.
    /// </summary>
    public bool CanProceedWithUnsavedChanges()
    {
        if (!_isDirty) return true;

        var choice = _dialogService.ShowUnsavedChangesConfirmation("le Concepteur de grille");

        if (choice == UnsavedChangesChoice.Discard) return true;
        if (choice == UnsavedChangesChoice.Cancel) return false;

        // Save
        if (!ValidateDesignerState(out var errorMessage))
        {
            _dialogService.ShowMessage(errorMessage, "Rubrique incomplète", System.Windows.MessageBoxImage.Warning);
            return false;
        }

        if (TotalWeight != 100 && !_dialogService.ShowConfirmation(
                $"Le total des poids est de {TotalWeight} %. Voulez-vous enregistrer quand même ?",
                "Poids non équilibrés"))
        {
            return false;
        }

        var rubric = BuildRubric();
        if (!_rubricService.SaveRubric(SessionName, CourseName, WorkName, rubric, out var saveError))
        {
            _dialogService.ShowMessage(
                $"La rubrique n'a pas pu être enregistrée.\n\n{saveError}",
                "Erreur de sauvegarde",
                System.Windows.MessageBoxImage.Error);
            return false;
        }

        HasExistingRubric = true;
        _isDirty = false;
        return true;
    }

    [RelayCommand]
    private void GoBack()
    {
        if (!CanProceedWithUnsavedChanges()) return;
        _navigationService.NavigateBack();
    }
}

internal partial class RubricCriterionEditorModel : ObservableObject
{
    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private int _weight;

    [ObservableProperty]
    private ObservableCollection<RubricScaleItemEditorModel> _scale = new();
}

internal partial class RubricScaleItemEditorModel : ObservableObject
{
    [ObservableProperty]
    private string _qualitative = string.Empty;

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private int _points;
}

internal partial class RubricPenaltyEditorModel : ObservableObject
{
    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private double _factor;

    [ObservableProperty]
    private double _min;
}