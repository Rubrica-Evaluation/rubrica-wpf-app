namespace GradingTool.Services;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.IO;
using GradingTool.Models;

public class CommentService : ICommentService
{
    private readonly Dictionary<string, List<CommentEntry>> _commentsByCriteria;
    private const string CommentsFileName = "reusable_comments.json";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
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
            string filePath = Path.Combine(gradingPath, CommentsFileName);
            string jsonContent = JsonSerializer.Serialize(_commentsByCriteria, _jsonOptions);
            await File.WriteAllTextAsync(filePath, jsonContent);
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
            if (!File.Exists(filePath)) return;

            string jsonContent = await File.ReadAllTextAsync(filePath);
            var loaded = TryDeserializeNewFormat(jsonContent) ?? MigrateFromLegacyFormat(jsonContent);
            if (loaded == null) return;

            MergeIntoCache(loaded);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des commentaires: {ex.Message}");
        }
    }

    private static Dictionary<string, List<CommentEntry>>? TryDeserializeNewFormat(string jsonContent)
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, List<CommentEntry>>>(jsonContent, _jsonOptions); }
        catch (JsonException) { return null; }
    }

    private static Dictionary<string, List<CommentEntry>>? MigrateFromLegacyFormat(string jsonContent)
    {
        var oldFormat = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonContent);
        if (oldFormat == null) return null;

        return oldFormat.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Select(t => new CommentEntry { Text = t, Severity = CommentSeverity.Aucun }).ToList(),
            StringComparer.OrdinalIgnoreCase);
    }

    private void MergeIntoCache(Dictionary<string, List<CommentEntry>> loaded)
    {
        foreach (var (criterionLabel, entries) in loaded)
        {
            if (!_commentsByCriteria.TryGetValue(criterionLabel, out var list))
            {
                list = new List<CommentEntry>();
                _commentsByCriteria[criterionLabel] = list;
            }

            foreach (var entry in entries)
            {
                if (!list.Any(e => string.Equals(e.Text, entry.Text, StringComparison.OrdinalIgnoreCase)))
                    list.Add(entry);
            }
        }
    }
}
