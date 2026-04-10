namespace GradingTool.Services;

using System.Text.Json;
using System.Text.Encodings.Web;
using System.IO;
using GradingTool.Models;

public class CommentService : ICommentService
{
    private readonly Dictionary<string, List<CommentEntry>> _commentsByCriteria;
    private const string CommentsFileName = "reusable_comments.json";
    private string? _currentGradingPath;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public CommentService()
    {
        _commentsByCriteria = new Dictionary<string, List<CommentEntry>>(StringComparer.OrdinalIgnoreCase);
    }

    public List<CommentEntry> GetCommentsForCriterion(string criterionLabel)
    {
        if (string.IsNullOrWhiteSpace(criterionLabel))
            return new List<CommentEntry>();

        return _commentsByCriteria.TryGetValue(criterionLabel, out var entries)
            ? new List<CommentEntry>(entries)
            : new List<CommentEntry>();
    }

    public void AddCommentForCriterion(string criterionLabel, CommentEntry entry)
    {
        if (string.IsNullOrWhiteSpace(criterionLabel) || string.IsNullOrWhiteSpace(entry.Text))
            return;

        if (!_commentsByCriteria.TryGetValue(criterionLabel, out var list))
        {
            list = new List<CommentEntry>();
            _commentsByCriteria[criterionLabel] = list;
        }

        var alreadyExists = list.Any(e => string.Equals(e.Text, entry.Text, StringComparison.OrdinalIgnoreCase));
        if (!alreadyExists)
            list.Add(entry);
    }

    public void UpdateCommentForCriterion(string criterionLabel, string oldText, CommentEntry newEntry)
    {
        if (string.IsNullOrWhiteSpace(criterionLabel) || string.IsNullOrWhiteSpace(newEntry.Text)) return;
        if (!_commentsByCriteria.TryGetValue(criterionLabel, out var list)) return;
        int idx = list.FindIndex(e => string.Equals(e.Text, oldText, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) list[idx] = newEntry;
    }

    public void RemoveCommentForCriterion(string criterionLabel, string commentText)
    {
        if (_commentsByCriteria.TryGetValue(criterionLabel, out var list))
            list.RemoveAll(e => string.Equals(e.Text, commentText, StringComparison.OrdinalIgnoreCase));
    }

    public async Task SaveCommentsAsync(string gradingPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(gradingPath) || !Directory.Exists(gradingPath)) return;
            // Ne pas écraser le fichier si le chargement a échoué pour ce chemin
            if (!string.Equals(_currentGradingPath, gradingPath, StringComparison.OrdinalIgnoreCase)) return;

            string filePath = Path.Combine(gradingPath, CommentsFileName);
            string jsonContent = JsonSerializer.Serialize(_commentsByCriteria, _jsonOptions);
            await Helpers.FileHelper.WriteAllTextAtomicAsync(filePath, jsonContent);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors de la sauvegarde des commentaires: {ex.Message}");
        }
    }

    public async Task LoadCommentsAsync(string gradingPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(gradingPath) || !Directory.Exists(gradingPath)) return;
            string filePath = Path.Combine(gradingPath, CommentsFileName);

            if (!File.Exists(filePath))
            {
                // Fichier absent : nouveau TP, on repart à zéro
                _commentsByCriteria.Clear();
                _currentGradingPath = gradingPath;
                return;
            }

            string jsonContent = await File.ReadAllTextAsync(filePath);
            var loaded = JsonSerializer.Deserialize<Dictionary<string, List<CommentEntry>>>(jsonContent, _jsonOptions);

            // Vider le cache seulement après un chargement réussi
            _commentsByCriteria.Clear();
            _currentGradingPath = gradingPath;

            if (loaded != null)
                PopulateCache(loaded);
        }
        catch (Exception ex)
        {
            // En cas d'échec (fichier verrouillé par OneDrive, etc.),
            // on conserve les commentaires existants plutôt que de les perdre
            System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des commentaires: {ex.Message}");
        }
    }

    private void PopulateCache(Dictionary<string, List<CommentEntry>> source)
    {
        foreach (var (criterionLabel, entries) in source)
            _commentsByCriteria[criterionLabel] = new List<CommentEntry>(entries);
    }
}
