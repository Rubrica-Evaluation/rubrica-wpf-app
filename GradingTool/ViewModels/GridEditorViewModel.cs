using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GradingTool.Models;
using GradingTool.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Diagnostics;

namespace GradingTool.ViewModels;

public partial class GridEditorViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IGridService _gridService;
    private readonly IDialogService _dialogService;
    private readonly IPdfService _pdfService;
    private readonly ICommentService _commentService;

    [ObservableProperty]
    private string _groupName = string.Empty;

    [ObservableProperty]
    private string _sessionName = string.Empty;

    [ObservableProperty]
    private string _courseName = string.Empty;

    [ObservableProperty]
    private string _workName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<GridFileInfo> _gridFiles = new();

    [ObservableProperty]
    private GridFileInfo? _selectedGridFile;

    [ObservableProperty]
    private GridModel? _currentGrid;

    [ObservableProperty]
    private double? _totalPoints;

    [ObservableProperty]
    private double? _totalPenalties;

    [ObservableProperty]
    private CriterionModel? _selectedCriterion;

    [ObservableProperty]
    private string? _selectedFeedbackItem;

    public void SelectSuggestedComment(string? comment, CriterionModel? criterion)
    {
        if (comment == null || criterion == null)
            return;

        // Ajouter le commentaire s'il n'existe pas déjà
        if (!criterion.Feedback.Contains(comment))
        {
            criterion.Feedback.Add(comment);
        }
    }

    public string NavigationPath => $"{SessionName} / {CourseName} / {WorkName} / {GroupName}";
    
    public GridEditorViewModel(INavigationService navigationService, IGridService gridService, IDialogService dialogService, IPdfService pdfService, ICommentService commentService)
    {
        _navigationService = navigationService;
        _gridService = gridService;
        _dialogService = dialogService;
        _pdfService = pdfService;
        _commentService = commentService;
    }

    public void Initialize(GroupModel group, string gradingPath, string session, string course, string work)
    {
        SessionName = session;
        CourseName = course;
        WorkName = work;
        GroupName = group.DisplayName;
        GridFiles.Clear();

        var gridFileList = _gridService.LoadGridFiles(gradingPath);

        foreach (var gridFile in gridFileList)
        {
            GridFiles.Add(gridFile);
        }

        // Sélectionner automatiquement le premier fichier si disponible
        if (GridFiles.Count > 0)
        {
            SelectedGridFile = GridFiles[0];
        }
    }

    partial void OnSelectedGridFileChanged(GridFileInfo? value)
    {
        // Sauvegarder automatiquement la grille actuelle avant de changer
        if (CurrentGrid != null && SelectedGridFile != null)
        {
            _ = SaveCurrentGridAsync();
        }

        // Détacher les anciens event handlers
        if (CurrentGrid != null)
        {
            foreach (var criterion in CurrentGrid.Criteria)
            {
                criterion.PropertyChanged -= OnCriterionPropertyChanged;
            }
            foreach (var penalty in CurrentGrid.Penalties)
            {
                penalty.PropertyChanged -= OnPenaltyPropertyChanged;
            }
        }
        
        CurrentGrid = null;
        TotalPoints = null;
        TotalPenalties = null;
        
        if (value != null)
        {
            _ = LoadCurrentGridAsync(value.FilePath);
        }
    }

    private async Task LoadCurrentGridAsync(string filePath)
    {
        CurrentGrid = await _gridService.LoadGridAsync(filePath);
        
        // Charger les commentaires réutilisables depuis le répertoire grading
        var groupPath = Path.GetDirectoryName(filePath)!;
        var gradingPath = Path.GetDirectoryName(groupPath)!;  // Remonter au répertoire grading
        await _commentService.LoadCommentsAsync(gradingPath);
        
        // Attacher les event handlers pour recalculer le total quand les points changent
        if (CurrentGrid != null)
        {
            foreach (var criterion in CurrentGrid.Criteria)
            {
                criterion.PropertyChanged += OnCriterionPropertyChanged;
                // Charger les suggestions pour ce critère
                await UpdateSuggestionsForCriterion(criterion);
            }
            foreach (var penalty in CurrentGrid.Penalties)
            {
                penalty.PropertyChanged += OnPenaltyPropertyChanged;
            }
        }
        
        RecalculateAll();
    }

    private void OnCriterionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CriterionModel.Points))
        {
            RecalculateAll();
        }
    }

    private void OnPenaltyPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PenaltyItemModel.Count) || e.PropertyName == nameof(PenaltyItemModel.Reason))
        {
            RecalculateAll();
        }
    }

    public async Task SaveCurrentGridAsync()
    {
        if (CurrentGrid != null && SelectedGridFile != null)
        {
            // Recalculer les points avant la sauvegarde
            RecalculateAll();

            // Le basePath doit être le répertoire du travail (ex: TP1), pas le répertoire grading
            var basePath = Path.GetDirectoryName(Path.GetDirectoryName(SelectedGridFile.FilePath))!;
            await _gridService.SaveGridAsync(CurrentGrid, basePath);
            await _commentService.SaveCommentsAsync(basePath);
        }
    }

    private void RecalculateAll()
    {
        if (CurrentGrid == null) return;

        // Calculer les points des critères
        foreach (var criterion in CurrentGrid.Criteria)
        {
            if (!string.IsNullOrEmpty(criterion.Result))
            {
                var selectedScale = criterion.Scale.FirstOrDefault(s => s.Qualitative == criterion.Result);
                if (selectedScale != null)
                {
                    // Calcul des points : points du niveau * poids / 100
                    criterion.Points = (double)selectedScale.Points * criterion.Weight / 100.0;
                }
            }
            else
            {
                criterion.Points = null;
            }
        }

        // Calculer le total des points (critères + pénalités)
        var criteriaTotal = CurrentGrid.Criteria.Sum(c => c.Points ?? 0);
        var penaltiesTotal = CurrentGrid.Penalties.Sum(p => p.ComputedPenalty);
        TotalPoints = criteriaTotal + penaltiesTotal;
        TotalPenalties = penaltiesTotal;
        CurrentGrid.Computed.Total = TotalPoints;
    }

    private async Task UpdateSuggestionsForCriterion(CriterionModel criterion)
    {
        criterion.SuggestedComments.Clear();
        
        // Ajouter le placeholder en premier
        criterion.SuggestedComments.Add("-- Commentaires existants --");
        
        // Charger les suggestions basées sur le label du critère
        var suggestions = _commentService.GetCommentsForCriterion(criterion.Label);
        
        foreach (var comment in suggestions)
        {
            criterion.SuggestedComments.Add(comment);
        }
        
        await Task.CompletedTask;
    }

    [RelayCommand]
    public void AddFeedback(CriterionModel? criterion)
    {
        if (criterion == null) return;

        SelectedCriterion = criterion;
        criterion.IsEditingFeedback = true;
        criterion.EditingFeedbackIndex = -1;
        criterion.FeedbackInput = string.Empty;
    }

    [RelayCommand]
    public void EditFeedback(CriterionModel? criterion)
    {
        if (criterion == null || SelectedFeedbackItem == null || criterion.Feedback.Count == 0)
            return;

        SelectedCriterion = criterion;
        criterion.EditingFeedbackIndex = criterion.Feedback.IndexOf(SelectedFeedbackItem);
        criterion.FeedbackInput = SelectedFeedbackItem;
        criterion.IsEditingFeedback = true;
    }

    [RelayCommand]
    public async Task ConfirmFeedback(CriterionModel? criterion)
    {
        if (criterion == null || string.IsNullOrWhiteSpace(criterion.FeedbackInput))
            return;

        if (criterion.EditingFeedbackIndex < 0)
        {
            criterion.Feedback.Add(criterion.FeedbackInput);
            
            // Sauvegarder le nouveau commentaire dans la banque réutilisable
            _commentService.AddCommentForCriterion(criterion.Label, criterion.FeedbackInput);
            // Note: La sauvegarde se fera lors de SaveAsync
        }
        else if (criterion.EditingFeedbackIndex < criterion.Feedback.Count)
        {
            criterion.Feedback[criterion.EditingFeedbackIndex] = criterion.FeedbackInput;
        }

        CancelFeedback(criterion);
        await Task.CompletedTask;
    }

    [RelayCommand]
    public void DeleteFeedback(CriterionModel? criterion)
    {
        if (criterion == null || SelectedFeedbackItem == null)
            return;

        criterion.Feedback.Remove(SelectedFeedbackItem);
    }

    [RelayCommand]
    public void CancelFeedback(CriterionModel? criterion)
    {
        if (criterion == null) return;

        criterion.IsEditingFeedback = false;
        criterion.FeedbackInput = string.Empty;
        criterion.EditingFeedbackIndex = -1;
        SelectedCriterion = null;
        SelectedFeedbackItem = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (CurrentGrid == null)
        {
            _dialogService.ShowToast("Erreur: Aucune grille chargée");
            return;
        }
        
        if (SelectedGridFile == null)
        {
            _dialogService.ShowToast("Erreur: Aucun fichier sélectionné");
            return;
        }

        try
        {
            // Recalculer les points avant la sauvegarde
            RecalculateAll();

            // Le basePath doit être le répertoire du travail (ex: TP1), pas le répertoire grading
            var basePath = Path.GetDirectoryName(Path.GetDirectoryName(SelectedGridFile.FilePath))!;
            var success = await _gridService.SaveGridAsync(CurrentGrid, basePath);
            
            if (success)
            {
                // Sauvegarder les commentaires réutilisables dans le répertoire grading
                var groupPath = Path.GetDirectoryName(SelectedGridFile.FilePath)!;
                var gradingPath = Path.GetDirectoryName(groupPath)!;  // Remonter au répertoire grading
                await _commentService.SaveCommentsAsync(gradingPath);
                
                _dialogService.ShowToast("Sauvegarde réussie");
                
                // Stocker le chemin du fichier actuel avant de recharger
                var currentFilePath = SelectedGridFile.FilePath;
                
                // Recharger la liste des fichiers pour mettre à jour les informations
                var gridFileList = _gridService.LoadGridFiles(groupPath);
                GridFiles.Clear();
                foreach (var gridFile in gridFileList)
                {
                    GridFiles.Add(gridFile);
                }
                // Resélectionner le fichier actuel
                SelectedGridFile = GridFiles.FirstOrDefault(g => g.FilePath == currentFilePath);
                
                if (SelectedGridFile == null)
                {
                    _dialogService.ShowToast("Sauvegarde réussie, mais fichier non trouvé après rechargement");
                }
            }
            else
            {
                _dialogService.ShowToast("Erreur lors de la sauvegarde");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowToast($"Erreur: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        // Sauvegarder automatiquement avant de quitter
        if (CurrentGrid != null && SelectedGridFile != null)
        {
            await SaveCurrentGridAsync();
        }

        SelectedGridFile = null;
        _navigationService.NavigateBack();
    }

    [RelayCommand]
    private async Task ExportSummary()
    {
        if (SelectedGridFile == null)
        {
            _dialogService.ShowToast("Erreur: Aucun groupe sélectionné");
            return;
        }

        try
        {
            // Get the group directory
            string groupDir = Path.GetDirectoryName(SelectedGridFile.FilePath)!;
            if (!Directory.Exists(groupDir))
            {
                _dialogService.ShowToast("Erreur: Dossier du groupe introuvable");
                return;
            }

            // Get all JSON files in the group directory
            var jsonFiles = Directory.GetFiles(groupDir, "*.json");
            if (jsonFiles.Length == 0)
            {
                _dialogService.ShowToast("Aucune grille trouvée dans le groupe");
                return;
            }

            // Load all grids and collect da and total
            var summaries = new List<(string da, double? total)>();
            foreach (var file in jsonFiles)
            {
                try
                {
                    var grid = await _gridService.LoadGridAsync(file);
                    if (grid?.Meta?.Student?.Da != null)
                    {
                        summaries.Add((grid.Meta.Student.Da, grid.Computed?.Total));
                    }
                }
                catch
                {
                    // Skip invalid files
                }
            }

            if (summaries.Count == 0)
            {
                _dialogService.ShowToast("Aucune grille valide trouvée");
                return;
            }

            // Sort by DA
            summaries = summaries.OrderBy(s => s.da).ToList();

            // Create CSV content
            var csvLines = new List<string> { "da,total" };
            foreach (var (da, total) in summaries)
            {
                csvLines.Add($"{da},{total?.ToString() ?? ""}");
            }

            // Save to sommaire_gr<group>.csv in the grading directory (parent of groupDir)
            string gradingDir = Path.GetDirectoryName(groupDir)!;
            string csvFileName = $"sommaire_{SelectedGridFile.Group}.csv";
            string csvPath = Path.Combine(gradingDir, csvFileName);

            await File.WriteAllLinesAsync(csvPath, csvLines);

            _dialogService.ShowToast($"Sommaire exporté: {csvPath}");

            // Open the folder
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = gradingDir,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore if opening folder fails
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowToast($"Erreur lors de l'export: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        if (SelectedGridFile == null)
        {
            _dialogService.ShowToast("Erreur: Aucun groupe sélectionné");
            return;
        }

        try
        {
            // Get the group directory
            string groupDir = Path.GetDirectoryName(SelectedGridFile.FilePath)!;
            if (!Directory.Exists(groupDir))
            {
                _dialogService.ShowToast("Erreur: Dossier du groupe introuvable");
                return;
            }

            // Get the pdf_docs path
            string workDir = Path.GetDirectoryName(Path.GetDirectoryName(groupDir))!;
            string pdfDocsDir = Path.Combine(workDir, "pdf_docs", SelectedGridFile.Group);

            var success = await _pdfService.ExportGroupPdfsAsync(groupDir, pdfDocsDir);

            if (success)
            {
                _dialogService.ShowToast("Exportation PDF réussie");
            }
            else
            {
                _dialogService.ShowToast("Erreur lors de l'exportation PDF");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowToast($"Erreur: {ex.Message}");
        }
    }

}