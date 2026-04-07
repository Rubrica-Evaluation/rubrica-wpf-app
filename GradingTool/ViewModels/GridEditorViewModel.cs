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

    private string _gradingRootPath = string.Empty;
    private string _groupGradingPath = string.Empty;

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
    private CommentEntry? _selectedFeedbackItem;

    [ObservableProperty]
    private bool _justSaved;

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
        _groupGradingPath = gradingPath;
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
                criterion.Feedback.CollectionChanged -= (s, e) => RefreshResultRecommendation(criterion);
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
        _gradingRootPath = gradingPath;
        await _commentService.LoadCommentsAsync(gradingPath);
        
        // Attacher les event handlers pour recalculer le total quand les points changent
        if (CurrentGrid != null)
        {
            foreach (var criterion in CurrentGrid.Criteria)
            {
                criterion.PropertyChanged += OnCriterionPropertyChanged;
                // Abonner aux changements de feedback pour la recommandation
                var capturedCriterion = criterion;
                capturedCriterion.Feedback.CollectionChanged += (s, e) =>
                    RefreshResultRecommendation(capturedCriterion);
                // Calculer la recommandation initiale
                RefreshResultRecommendation(criterion);
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
            _ = ShowSavedFeedbackAsync();
        }
    }

    private async Task ShowSavedFeedbackAsync()
    {
        JustSaved = true;
        await Task.Delay(2000);
        JustSaved = false;
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

    private void RefreshResultRecommendation(CriterionModel criterion)
    {
        criterion.RecommendedResult = _gridService.GetResultRecommendation(
            criterion.Feedback, criterion.Scale);
    }

    public string GetCommentUsagesTooltip(CommentEntry entry)
    {
        if (string.IsNullOrEmpty(_gradingRootPath)) return string.Empty;
        var usages = _gridService.FindCommentUsages(_gradingRootPath, entry);
        if (usages.Count == 0) return "Aucune grille ne contient ce commentaire.";
        return "Utilisé dans :\n" + string.Join("\n", usages.Select(u => $"• {u}"));
    }

    [RelayCommand]
    public void OpenCommentPicker(CriterionModel? criterion)
    {
        if (criterion == null) return;

        var comments = _commentService.GetCommentsForCriterion(criterion.Label);
        var selected = Views.CommentPickerDialog.Show(criterion.Label, comments, _commentService);

        if (selected != null && !criterion.Feedback.Any(f => string.Equals(f.Text, selected, StringComparison.OrdinalIgnoreCase)))
        {
            var bankEntry = comments.FirstOrDefault(c => string.Equals(c.Text, selected, StringComparison.OrdinalIgnoreCase));
            criterion.Feedback.Add(bankEntry ?? new CommentEntry { Text = selected, Severity = CommentSeverity.Aucun });
        }
    }

    [RelayCommand]
    public void ApplyRecommendation(CriterionModel? criterion)
    {
        if (criterion == null || string.IsNullOrEmpty(criterion.RecommendedResult)) return;
        criterion.Result = criterion.RecommendedResult;
    }

    [RelayCommand]
    public void AddFeedback(CriterionModel? criterion)
    {
        if (criterion == null) return;

        var (text, addToBank, severity) = Views.InputDialog.ShowWithBankOption("Nouveau commentaire :", "Ajouter un commentaire", multiline: true);
        if (string.IsNullOrWhiteSpace(text)) return;

        var entry = new CommentEntry { Text = text, Severity = severity };
        criterion.Feedback.Add(entry);
        if (addToBank)
            _commentService.AddCommentForCriterion(criterion.Label, entry);
    }

    [RelayCommand]
    public void EditFeedback(CriterionModel? criterion)
    {
        if (criterion == null || SelectedFeedbackItem == null || criterion.Feedback.Count == 0)
            return;

        int idx = criterion.Feedback.IndexOf(SelectedFeedbackItem);
        var originalEntry = SelectedFeedbackItem;
        var (text, severity, updateBank) = Views.InputDialog.ShowWithSeverity("Modifier le commentaire :", "Modifier", originalEntry.Text, originalEntry.Severity);
        if (text == null) return;

        var updatedEntry = new CommentEntry { Text = text, Severity = severity };
        if (idx >= 0 && idx < criterion.Feedback.Count)
            criterion.Feedback[idx] = updatedEntry;

        if (updateBank)
            _commentService.UpdateCommentForCriterion(criterion.Label, originalEntry.Text, updatedEntry);

        SelectedFeedbackItem = null;
    }

    [RelayCommand]
    public void DeleteFeedback(CriterionModel? criterion)
    {
        if (criterion == null || SelectedFeedbackItem == null)
            return;

        criterion.Feedback.Remove(SelectedFeedbackItem);
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
                _ = ShowSavedFeedbackAsync();
                
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
    private async Task ExportSinglePdf()
    {
        if (CurrentGrid == null || SelectedGridFile == null)
        {
            _dialogService.ShowToast("Aucune grille sélectionnée");
            return;
        }

        try
        {
            string groupDir = Path.GetDirectoryName(SelectedGridFile.FilePath)!;
            string workDir = Path.GetDirectoryName(Path.GetDirectoryName(groupDir))!;
            string pdfDocsDir = Path.Combine(workDir, "pdf_docs", SelectedGridFile.Group);
            Directory.CreateDirectory(pdfDocsDir);

            string pdfFileName = Path.GetFileNameWithoutExtension(SelectedGridFile.FilePath) + ".pdf";
            string pdfPath = Path.Combine(pdfDocsDir, pdfFileName);

            if (File.Exists(pdfPath))
            {
                var choice = _dialogService.ShowOverwriteConfirmation(1, 1);
                if (choice == OverwriteChoice.Cancel)
                    return;
                if (choice == OverwriteChoice.Skip)
                {
                    Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = pdfDocsDir, UseShellExecute = true });
                    return;
                }
            }

            var success = await _pdfService.ExportPdfAsync(CurrentGrid, pdfPath);
            if (success)
            {
                _dialogService.ShowToast("PDF exporté avec succès");
                Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = pdfDocsDir, UseShellExecute = true });
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

            int totalCount = Directory.GetFiles(groupDir, "*.json").Length;
            int existingCount = Directory.Exists(pdfDocsDir)
                ? Directory.GetFiles(pdfDocsDir, "*.pdf").Length
                : 0;

            bool overwrite = false;
            if (existingCount > 0)
            {
                var choice = _dialogService.ShowOverwriteConfirmation(existingCount, totalCount);
                if (choice == OverwriteChoice.Cancel)
                    return;
                overwrite = choice == OverwriteChoice.Overwrite;
            }

            var success = await _pdfService.ExportGroupPdfsAsync(groupDir, pdfDocsDir, overwrite);

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

    private bool CanDeleteGrid() => SelectedGridFile != null;

    [RelayCommand(CanExecute = nameof(CanDeleteGrid))]
    private void DeleteGrid()
    {
        if (SelectedGridFile == null)
            return;

        var confirmed = _dialogService.ShowConfirmation(
            $"Voulez-vous vraiment supprimer la grille de {SelectedGridFile.DisplayName} ?\n\nCette action est irréversible.",
            "Supprimer la grille");

        if (!confirmed)
            return;

        try
        {
            File.Delete(SelectedGridFile.FilePath);
            GridFiles.Remove(SelectedGridFile);
            SelectedGridFile = GridFiles.Count > 0 ? GridFiles[0] : null;
        }
        catch (Exception ex)
        {
            _dialogService.ShowToast($"Erreur lors de la suppression: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenGradingFolder()
    {
        var folder = string.IsNullOrEmpty(_groupGradingPath)
            ? (SelectedGridFile != null ? Path.GetDirectoryName(SelectedGridFile.FilePath) : null)
            : _groupGradingPath;

        if (string.IsNullOrEmpty(folder))
            return;

        try
        {
            Directory.CreateDirectory(folder);
            Process.Start("explorer.exe", folder);
        }
        catch (Exception ex)
        {
            _dialogService.ShowToast($"Impossible d'ouvrir le dossier: {ex.Message}");
        }
    }

}