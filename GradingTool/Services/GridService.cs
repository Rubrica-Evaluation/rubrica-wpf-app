using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using GradingTool.Models;
using GradingTool.Helpers;
using Microsoft.VisualBasic.FileIO;

namespace GradingTool.Services;

public class GridService : IGridService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public GridModel GenerateGrid(StudentModel student, RubricModel rubric, string tpName)
    {
        var grid = new GridModel
        {
            Meta = new GridMeta
            {
                Tp = tpName,
                Student = new StudentModel
                {
                    Da = student.Da,
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    Group = student.Group,
                    GroupCode = student.GroupCode,
                    Team = student.Team
                }
                // Members is null for individual students
            },
            Penalties = MapPenalties(rubric),
            Criteria = MapCriteria(rubric),
            Computed = new ComputedModel { Total = null }
        };

        return grid;
    }

    public async Task<bool> SaveGridAsync(GridModel grid, string basePath)
    {
        try
        {
            var gradingPath = Path.Combine(basePath, grid.Meta.Student.GroupCode ?? string.Empty);
            Directory.CreateDirectory(gradingPath);

            var fileName = GenerateFileName(grid.Meta.Student);
            var filePath = Path.Combine(gradingPath, fileName);

            var json = JsonSerializer.Serialize(grid, _jsonOptions);
            await FileHelper.WriteAllTextAtomicAsync(filePath, json);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateGridAsync(GridModel grid, string filePath)
    {
        try
        {
            var json = JsonSerializer.Serialize(grid, _jsonOptions);
            await FileHelper.WriteAllTextAtomicAsync(filePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool GridExists(StudentModel student, string basePath)
    {
        var gradingPath = Path.Combine(basePath, student.GroupCode ?? string.Empty);
        var fileName = GenerateFileName(student);
        var filePath = Path.Combine(gradingPath, fileName);

        return File.Exists(filePath);
    }

    private string GenerateFileName(StudentModel student)
    {
        var teamPrefix = student.Team > 0 ? $"T{student.Team}_" : "";
        
        // Nettoyer chaque partie du nom séparément
        var cleanFirstName = SanitizeFileNamePart(student.FirstName);
        var cleanLastName = SanitizeFileNamePart(student.LastName);
        var cleanDa = SanitizeFileNamePart(student.Da);
        
        var sanitizedName = $"{cleanFirstName}_{cleanLastName}_{cleanDa}";
        return $"{teamPrefix}{sanitizedName}.json";
    }

    private string SanitizeFileNamePart(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "Unknown";
            
        // Remplacer les caractères problématiques par des underscores
        // Liste des caractères interdits dans les noms de fichiers Windows
        var invalidChars = new[] { '<', '>', ':', '"', '|', '?', '*', '\\', '/', '\0', '\'' };
        var sanitized = input;
        
        foreach (var invalidChar in invalidChars)
        {
            sanitized = sanitized.Replace(invalidChar.ToString(), "_");
        }
        
        // Remplacer également les espaces et tirets par des underscores
        sanitized = sanitized.Replace(" ", "_").Replace("-", "_");
        
        // Supprimer les caractères de contrôle et autres caractères problématiques
        sanitized = new string(sanitized.Where(c => 
            !char.IsControl(c) && 
            c != '\r' && 
            c != '\n' && 
            c != '\t').ToArray());
        
        // S'assurer que ce n'est pas vide après nettoyage
        if (string.IsNullOrWhiteSpace(sanitized))
            return "Unknown";
            
        // Limiter la longueur pour éviter les noms de fichiers trop longs
        if (sanitized.Length > 50)
            sanitized = sanitized.Substring(0, 50);
            
        return sanitized;
    }

    public GridModel GenerateTeamGrid(List<StudentModel> teamStudents, RubricModel rubric, string tpName, int teamNumber)
    {
        if (teamStudents == null || teamStudents.Count == 0)
            throw new ArgumentException("La liste d'étudiants de l'équipe ne peut pas être vide.", nameof(teamStudents));

        // Utiliser le premier étudiant comme référence
        var firstStudent = teamStudents[0];

        var grid = new GridModel
        {
            Meta = new GridMeta
            {
                Tp = tpName,
                Student = new StudentModel
                {
                    Da = string.Empty,  // Pas de DA individuel pour une équipe
                    FirstName = $"T{teamNumber}",
                    LastName = string.Empty,
                    Group = firstStudent.Group,
                    GroupCode = firstStudent.GroupCode,
                    Team = teamNumber
                },
                Members = teamStudents.Select(s => new GridMemberModel
                {
                    Da = s.Da,
                    FirstName = s.FirstName,
                    LastName = s.LastName
                }).ToList()
            },
            Penalties = MapPenalties(rubric),
            Criteria = MapCriteria(rubric),
            Computed = new ComputedModel { Total = null }
        };

        return grid;
    }

    public bool TeamGridExists(List<StudentModel> teamStudents, string basePath, int teamNumber)
    {
        if (teamStudents == null || teamStudents.Count == 0)
            return false;

        var firstStudent = teamStudents[0];
        var gradingPath = Path.Combine(basePath, firstStudent.GroupCode ?? string.Empty);
        var fileName = GenerateTeamFileName(teamStudents, teamNumber);
        var filePath = Path.Combine(gradingPath, fileName);

        return File.Exists(filePath);
    }

    private string GenerateTeamFileName(List<StudentModel> teamStudents, int teamNumber)
    {
        // Trier les étudiants par nom pour avoir un ordre stable
        var sortedStudents = teamStudents.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToList();
        
        // Créer une liste des noms + DA
        var memberNames = sortedStudents
            .Select(s => $"{SanitizeFileNamePart(s.FirstName)}_{SanitizeFileNamePart(s.LastName)}_{SanitizeFileNamePart(s.Da)}")
            .ToList();
        
        // Joindre avec des underscores
        var membersString = string.Join("_", memberNames);
        
        return $"T{teamNumber}_{membersString}.json";
    }

    public async Task<bool> SaveTeamGridAsync(GridModel grid, List<StudentModel> teamStudents, int teamNumber, string basePath)
    {
        try
        {
            var gradingPath = Path.Combine(basePath, grid.Meta.Student.GroupCode ?? string.Empty);
            Directory.CreateDirectory(gradingPath);

            var fileName = GenerateTeamFileName(teamStudents, teamNumber);
            var filePath = Path.Combine(gradingPath, fileName);

            var json = JsonSerializer.Serialize(grid, _jsonOptions);
            await FileHelper.WriteAllTextAtomicAsync(filePath, json);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public List<GridFileInfo> LoadGridFiles(string gradingPath)
    {
        if (!Directory.Exists(gradingPath))
            return new List<GridFileInfo>();

        return Directory.GetFiles(gradingPath, "*.json")
            .Select(TryLoadGridFileInfo)
            .OfType<GridFileInfo>()
            .OrderBy(g => g.TeamNumber == 0 ? int.MaxValue : g.TeamNumber)
            .ThenBy(g => g.FileName)
            .ToList();
    }

    private GridFileInfo? TryLoadGridFileInfo(string filePath)
    {
        try
        {
            var jsonContent = File.ReadAllText(filePath, Encoding.UTF8);
            var gridData = JsonSerializer.Deserialize<GridModel>(jsonContent, _jsonOptions);
            if (gridData?.Meta == null) return null;
            return BuildGridFileInfo(gridData, filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la lecture de {filePath}: {ex.Message}");
            return null;
        }
    }

    private static GridFileInfo BuildGridFileInfo(GridModel gridData, string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var info = new GridFileInfo
        {
            FilePath = filePath,
            FileName = fileName,
            TeamNumber = gridData.Meta.Student?.Team ?? 0,
            Members = gridData.Meta.Members ?? new List<GridMemberModel>(),
            Group = gridData.Meta.Student?.Group ?? "Groupe inconnu",
            Total = gridData.Computed?.Total ?? 0,
            PenaltyCounts = gridData.Penalties?.Select(p => p.Count).ToList() ?? new List<int>(),
            DisplayName = string.Empty
        };

        ResolveMembers(info, gridData, fileName);
        info.DisplayName = ResolveDisplayName(info);
        return info;
    }

    private static void ResolveMembers(GridFileInfo info, GridModel gridData, string fileName)
    {
        if (info.Members.Count == 0 && gridData.Meta.Student != null)
        {
            info.Members.Add(new GridMemberModel
            {
                Da = gridData.Meta.Student.Da,
                FirstName = gridData.Meta.Student.FirstName,
                LastName = gridData.Meta.Student.LastName
            });
        }

        if (info.Members.Count == 0 && info.TeamNumber > 0)
            ExtractMembersFromFileName(info, fileName);
    }

    private static void ExtractMembersFromFileName(GridFileInfo info, string fileName)
    {
        var cleanFileName = fileName.Replace($"T{info.TeamNumber}_", "").Replace(".json", "");
        var parts = cleanFileName.Split('_');
        for (int i = 0; i + 2 < parts.Length; i += 3)
        {
            info.Members.Add(new GridMemberModel
            {
                FirstName = parts[i],
                LastName = parts[i + 1],
                Da = parts[i + 2]
            });
        }
    }

    private static string ResolveDisplayName(GridFileInfo info)
    {
        if (info.TeamNumber > 0)
            return $"Équipe {info.TeamNumber}";
        if (info.Members.Count == 1)
            return info.Members[0].DisplayName;
        return "Groupe";
    }

    public async Task<GridModel?> LoadGridAsync(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var grid = await JsonSerializer.DeserializeAsync<GridModel>(stream, _jsonOptions);
            return grid;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors du chargement de la grille {filePath}: {ex.Message}");
            return null;
        }
    }

    private static List<PenaltyItemModel> MapPenalties(RubricModel rubric) =>
        rubric.Penalties.Select(p => new PenaltyItemModel
        {
            Label = p.Label,
            Count = p.Count,
            Factor = p.Factor,
            Reason = p.Reason,
            Min = p.Min
        }).ToList();

    private static List<CriterionModel> MapCriteria(RubricModel rubric) =>
        rubric.Criteria.Select(c => new CriterionModel
        {
            Label = c.Label,
            Scale = c.Scale,
            Weight = c.Weight,
            Result = c.Result,
            Feedback = new ObservableCollection<CommentEntry>(c.Feedback),
            Points = c.Points
        }).ToList();

    public string? GetResultRecommendation(
        IEnumerable<CommentEntry> feedback,
        IEnumerable<ScaleItemModel> scale)
    {
        var feedbackList = feedback.Where(f => !string.IsNullOrWhiteSpace(f.Text)).ToList();
        var scaleList = scale.OrderByDescending(s => s.Points).ToList();

        if (feedbackList.Count == 0 || scaleList.Count < 2)
            return null;

        int nMineur = 0, nMajeur = 0, nCritique = 0;
        foreach (var entry in feedbackList)
        {
            switch (entry.Severity)
            {
                case CommentSeverity.Mineur:   nMineur++;   break;
                case CommentSeverity.Majeur:   nMajeur++;   break;
                case CommentSeverity.Critique: nCritique++; break;
            }
        }

        int maxDrop = scaleList.Count - 2;
        int drop;
        if (nCritique >= 1)          drop = maxDrop;
        else if (nMajeur >= 2)       drop = 3;
        else if (nMajeur >= 1 || nMineur > 2) drop = 2;
        else if (nMineur >= 1)       drop = 1;
        else                         drop = 0;

        drop = Math.Min(drop, maxDrop);
        return scaleList[drop].Qualitative;
    }
    public bool GradingFolderHasFiles(string gradingBasePath, string groupCode)
    {
        var path = Path.Combine(gradingBasePath, groupCode);
        return Directory.Exists(path) && Directory.GetFiles(path, "*.json").Length > 0;
    }

    public void DeleteGradingFolder(string gradingBasePath, string groupCode)
    {
        var path = Path.Combine(gradingBasePath, groupCode);
        if (Directory.Exists(path))
            FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
    }
    public List<string> FindCommentUsages(string gradingPath, CommentEntry comment)
    {
        var usages = new List<string>();
        if (!Directory.Exists(gradingPath)) return usages;

        foreach (var groupDir in Directory.GetDirectories(gradingPath))
        {
            foreach (var filePath in Directory.GetFiles(groupDir, "*.json"))
            {
                try
                {
                    var jsonContent = File.ReadAllText(filePath, Encoding.UTF8);
                    var grid = JsonSerializer.Deserialize<GridModel>(jsonContent, _jsonOptions);
                    if (grid == null) continue;

                    bool hasMatch = grid.Criteria.Any(c =>
                        c.Feedback.Any(f =>
                            string.Equals(f.Text, comment.Text, StringComparison.Ordinal) &&
                            f.Severity == comment.Severity));

                    if (hasMatch)
                        usages.Add(BuildUsageLabel(grid));
                }
                catch { /* fichier illisible, on ignore */ }
            }
        }
        return usages;
    }

    private static string BuildUsageLabel(GridModel grid)
    {
        if (grid.Meta.Members is { Count: > 0 })
        {
            var names = string.Join(" / ", grid.Meta.Members.Select(m => m.DisplayName));
            return $"{grid.Meta.Student.Group} — {names}";
        }
        var student = grid.Meta.Student;
        var displayName = $"{student.FirstName} {student.LastName}".Trim();
        return $"{student.Group} — {displayName}";
    }
}

// Classe pour afficher les informations du fichier
public class GridFileInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int TeamNumber { get; set; }
    public List<GridMemberModel> Members { get; set; } = new();
    public string Group { get; set; } = string.Empty;
    public double Total { get; set; }
    public List<int> PenaltyCounts { get; set; } = new();
    public string DisplayName { get; set; } = string.Empty;
    
    public string TeamTitle => TeamNumber > 0 ? $"Équipe {TeamNumber}" : string.Empty;
    public bool IsTeam => Members.Count > 1 || (Members.Count == 1 && TeamNumber > 0);
    public string MembersDisplay => string.Join("\n", Members.Select(m => m.DisplayName));
    public string MembersWithDaDisplay => string.Join("\n", Members.Select(m => $"{m.FirstName} {m.LastName} ({m.Da})"));
    public string PenaltiesDisplay => "Voir détails";
    public string TotalDisplay => $"{Total:F2}%";
}