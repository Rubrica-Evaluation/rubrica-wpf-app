using GradingTool.Helpers;
using GradingTool.Models;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Collections.ObjectModel;

namespace GradingTool.Services;

public class RubricService : IRubricService
{
    private readonly ISessionsRootService _sessionsRootService;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public RubricService(ISessionsRootService sessionsRootService)
    {
        _sessionsRootService = sessionsRootService;
    }

    public bool RubricExists(string sessionName, string courseName, string workName)
    {
        var rubricPath = GetRubricPath(sessionName, courseName, workName);
        return File.Exists(rubricPath);
    }

    public string GetRubricPath(string sessionName, string courseName, string workName)
    {
        var rootPath = _sessionsRootService.GetSessionsRootPath();
        if (string.IsNullOrEmpty(rootPath))
        {
            throw new InvalidOperationException("Le dossier racine des sessions n'est pas configuré.");
        }
        return Path.Combine(rootPath, sessionName, courseName, workName, "rubric", "rubric.json");
    }

    public RubricModel? LoadRubric(string sessionName, string courseName, string workName, out string errorMessage)
    {
        errorMessage = string.Empty;
        var rubricPath = GetRubricPath(sessionName, courseName, workName);
        return LoadRubricFromFile(rubricPath, out errorMessage);
    }

    public RubricModel? LoadRubricFromFile(string filePath, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (!File.Exists(filePath))
        {
            errorMessage = "Le fichier rubric.json n'existe pas.";
            return null;
        }

        try
        {
            var jsonContent = File.ReadAllText(filePath, Encoding.UTF8);
            var rubric = JsonSerializer.Deserialize<RubricModel>(jsonContent);

            if (rubric == null)
            {
                errorMessage = "Impossible de désérialiser le fichier JSON.";
                return null;
            }

            if (!ValidateRubricFormat(rubric, out errorMessage))
            {
                return null;
            }

            return rubric;
        }
        catch (JsonException)
        {
            errorMessage = "Le fichier n'est pas au bon format JSON valide.";
            return null;
        }
        catch (Exception)
        {
            errorMessage = "Impossible de lire le fichier. Vérifiez qu'il n'est pas corrompu.";
            return null;
        }
    }

    public RubricModel CreateEmptyRubric(string workName)
    {
        return new RubricModel
        {
            Meta = new RubricMeta
            {
                Tp = workName,
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
            Penalties = new List<PenaltyItemModel>(),
            Criteria = new List<CriterionModel>(),
            Computed = new ComputedModel
            {
                Total = null
            }
        };
    }

    public bool SaveRubric(string sessionName, string courseName, string workName, RubricModel rubric, out string errorMessage)
    {
        if (!ValidateRubricFormat(rubric, out errorMessage))
        {
            return false;
        }

        try
        {
            var rubricPath = GetRubricPath(sessionName, courseName, workName);
            var rubricDirectory = Path.GetDirectoryName(rubricPath);

            if (!string.IsNullOrEmpty(rubricDirectory) && !Directory.Exists(rubricDirectory))
            {
                Directory.CreateDirectory(rubricDirectory);
            }

            var jsonContent = JsonSerializer.Serialize(rubric, JsonOptions);
            FileHelper.WriteAllTextAtomic(rubricPath, jsonContent, Encoding.UTF8);
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public bool ValidateRubricFormat(RubricModel rubric, out string errorMessage)
    {
        errorMessage = string.Empty;

        // Verify required sections exist
        if (rubric.Meta == null)
        {
            errorMessage = "La rubrique ne contient pas les informations de base (meta).";
            return false;
        }

        if (rubric.Meta.Student == null)
        {
            errorMessage = "La rubrique ne contient pas les informations d'étudiant.";
            return false;
        }

        if (rubric.Penalties == null)
        {
            errorMessage = "La rubrique ne contient pas la section des pénalités.";
            return false;
        }

        if (rubric.Criteria == null || rubric.Criteria.Count == 0)
        {
            errorMessage = "La rubrique doit contenir au moins un critère d'évaluation.";
            return false;
        }

        if (rubric.Computed == null)
        {
            errorMessage = "La rubrique ne contient pas la section de calculs.";
            return false;
        }

        // Validate each criterion has required fields
        for (int i = 0; i < rubric.Criteria.Count; i++)
        {
            var criterion = rubric.Criteria[i];
            var criterionName = $"Critère {i + 1}";

            if (string.IsNullOrEmpty(criterion.Label))
            {
                errorMessage = $"Le critère '{criterionName}' n'a pas de titre (label).";
                return false;
            }

            if (criterion.Scale == null || criterion.Scale.Count == 0)
            {
                errorMessage = $"Le critère '{criterionName}' n'a pas d'échelle de notation.";
                return false;
            }

            if (criterion.Weight <= 0)
            {
                errorMessage = $"Le critère '{criterionName}' doit avoir un poids supérieur à 0 (actuellement: {criterion.Weight}).";
                return false;
            }
        }

        return true;
    }

    public void ImportRubric(string sessionName, string courseName, string workName, string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Le fichier sélectionné n'existe pas ou n'est plus accessible.");
        }

        // Load and validate the rubric first
        try
        {
            var jsonContent = File.ReadAllText(sourceFilePath, Encoding.UTF8);
            var rubric = JsonSerializer.Deserialize<RubricModel>(jsonContent);

            if (rubric == null)
            {
                throw new InvalidOperationException("Le fichier n'est pas une rubrique valide. Assurez-vous d'utiliser le bon format.");
            }

            if (!ValidateRubricFormat(rubric, out string errorMessage))
            {
                throw new InvalidOperationException($"La rubrique n'est pas complète: {errorMessage}");
            }
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Le fichier n'est pas au bon format JSON. Téléchargez le template pour voir un exemple de format valide.");
        }

        // Copy to destination
        var destinationPath = GetRubricPath(sessionName, courseName, workName);
        var destinationDir = Path.GetDirectoryName(destinationPath);
        
        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        File.Copy(sourceFilePath, destinationPath, overwrite: true);
    }

    public void SaveRubricTemplate(string destinationFilePath)
    {
        var template = new RubricModel
        {
            Meta = new RubricMeta
            {
                Tp = "TP1",
                Student = new StudentModel
                {
                    Da = "",
                    FirstName = "",
                    LastName = "",
                    Group = "",
                    GroupCode = "",
                    Team = 0
                }
            },
            Penalties = new List<PenaltyItemModel>
            {
                new PenaltyItemModel
                {
                    Label = "Nombre de jours de retard",
                    Count = 0,
                    Factor = -10,
                    Reason = "",
                    Min = -30
                },
                new PenaltyItemModel
                {
                    Label = "Nombre d'erreurs de langue",
                    Count = 0,
                    Factor = -0.25,
                    Reason = "",
                    Min = -20
                },
                new PenaltyItemModel
                {
                    Label = "Respect des contraintes de l'énoncé",
                    Count = 0,
                    Factor = -5,
                    Reason = "",
                    Min = -30
                }
            },
            Criteria = new List<CriterionModel>
            {
                new CriterionModel
                {
                    Label = "Structure des tables",
                    Scale = new List<ScaleItemModel>
                    {
                        new ScaleItemModel { Qualitative = "A", Label = "Structure parfaite, toutes les tables correctement définies", Points = 100 },
                        new ScaleItemModel { Qualitative = "B", Label = "Structure bonne, quelques détails mineurs à améliorer", Points = 80 },
                        new ScaleItemModel { Qualitative = "C", Label = "Structure acceptable, nécessite des améliorations", Points = 60 },
                        new ScaleItemModel { Qualitative = "D", Label = "Structure insuffisante, problèmes importants", Points = 40 },
                        new ScaleItemModel { Qualitative = "E", Label = "Structure inexistante ou complètement erronée", Points = 0 }
                    },
                    Weight = 60,
                    Result = "",
                    Feedback = new ObservableCollection<CommentEntry>(),
                    Points = null
                },
                new CriterionModel
                {
                    Label = "Types de données",
                    Scale = new List<ScaleItemModel>
                    {
                        new ScaleItemModel { Qualitative = "A", Label = "Tous les types de données sont appropriés et optimisés", Points = 100 },
                        new ScaleItemModel { Qualitative = "B", Label = "Types généralement appropriés, quelques optimisations possibles", Points = 80 },
                        new ScaleItemModel { Qualitative = "C", Label = "Types acceptables mais nécessitent des améliorations", Points = 60 },
                        new ScaleItemModel { Qualitative = "D", Label = "Types inadéquats, problèmes significatifs", Points = 40 },
                        new ScaleItemModel { Qualitative = "E", Label = "Types incorrects ou absents", Points = 0 }
                    },
                    Weight = 40,
                    Result = "",
                    Feedback = new ObservableCollection<CommentEntry>(),
                    Points = null
                }
            },
            Computed = new ComputedModel
            {
                Total = null
            }
        };

        var jsonContent = JsonSerializer.Serialize(template, JsonOptions);
        File.WriteAllText(destinationFilePath, jsonContent, Encoding.UTF8);
    }
}
