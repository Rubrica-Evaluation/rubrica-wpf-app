namespace GradingTool.Services;

using System.Text.Json;
using System.Text.Encodings.Web;
using System.IO;

public class CommentService : ICommentService
{
    private readonly Dictionary<string, List<string>> _commentsByCriteria;
    private const string CommentsFileName = "reusable_comments.json";

    public CommentService()
    {
        // Initialiser avec un dictionnaire vide
        // Les commentaires seront chargés depuis le fichier JSON lors du démarrage
        _commentsByCriteria = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    }

    public List<string> GetCommentsForCriterion(string criterionLabel)
    {
        if (string.IsNullOrWhiteSpace(criterionLabel))
        {
            return new List<string>();
        }

        if (_commentsByCriteria.TryGetValue(criterionLabel, out var comments))
        {
            return new List<string>(comments); // Retourner une copie
        }

        // Retourner une liste vide si le critère n'existe pas
        return new List<string>();
    }

    public void AddCommentForCriterion(string criterionLabel, string comment)
    {
        if (string.IsNullOrWhiteSpace(criterionLabel) || string.IsNullOrWhiteSpace(comment))
        {
            return;
        }

        if (!_commentsByCriteria.ContainsKey(criterionLabel))
        {
            _commentsByCriteria[criterionLabel] = new List<string>();
        }

        // Ajouter seulement si le commentaire n'existe pas déjà
        if (!_commentsByCriteria[criterionLabel].Contains(comment))
        {
            _commentsByCriteria[criterionLabel].Add(comment);
        }
    }

    public async Task SaveCommentsAsync(string gradingPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(gradingPath) || !Directory.Exists(gradingPath))
            {
                return;
            }

            string filePath = Path.Combine(gradingPath, CommentsFileName);
            
            // Sérialiser le dictionnaire des commentaires avec accents préservés
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string jsonContent = JsonSerializer.Serialize(_commentsByCriteria, options);
            
            // Écrire dans le fichier
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
            if (string.IsNullOrWhiteSpace(gradingPath) || !Directory.Exists(gradingPath))
            {
                return;
            }

            string filePath = Path.Combine(gradingPath, CommentsFileName);
            
            if (!File.Exists(filePath))
            {
                return;
            }

            // Lire le fichier JSON
            string jsonContent = await File.ReadAllTextAsync(filePath);
            
            // Désérialiser et fusionner avec les commentaires existants
            var loadedComments = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonContent);
            
            if (loadedComments != null)
            {
                foreach (var kvp in loadedComments)
                {
                    if (!_commentsByCriteria.ContainsKey(kvp.Key))
                    {
                        _commentsByCriteria[kvp.Key] = new List<string>();
                    }
                    
                    // Ajouter les commentaires chargés s'ils n'existent pas déjà
                    foreach (var comment in kvp.Value)
                    {
                        if (!_commentsByCriteria[kvp.Key].Contains(comment))
                        {
                            _commentsByCriteria[kvp.Key].Add(comment);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des commentaires: {ex.Message}");
        }
    }
}
