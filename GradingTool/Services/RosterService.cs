using GradingTool.Models;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

namespace GradingTool.Services;

public class RosterService : IRosterService
{
    private readonly ISessionsRootService _sessionsRootService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public RosterService(ISessionsRootService sessionsRootService)
    {
        _sessionsRootService = sessionsRootService;
    }

    private string GetRosterFilePath(string sessionName, string courseName, string workName)
    {
        var rootPath = _sessionsRootService.GetSessionsRootPath();
        if (string.IsNullOrEmpty(rootPath))
            throw new InvalidOperationException("Le dossier racine des sessions n'\''est pas configure.");
        return Path.Combine(rootPath, sessionName, courseName, workName, "roster", "roster.json");
    }

    public bool RosterExists(string sessionName, string courseName, string workName)
    {
        try
        {
            return File.Exists(GetRosterFilePath(sessionName, courseName, workName));
        }
        catch
        {
            return false;
        }
    }

    public RosterModel? LoadRoster(string sessionName, string courseName, string workName, out string errorMessage)
    {
        errorMessage = string.Empty;
        try
        {
            var filePath = GetRosterFilePath(sessionName, courseName, workName);
            if (!File.Exists(filePath))
            {
                errorMessage = "Le fichier roster.json n'\''existe pas.";
                return null;
            }
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            var roster = JsonSerializer.Deserialize<RosterModel>(json, JsonOptions);
            if (roster == null)
            {
                errorMessage = "Le fichier roster.json ne peut pas etre deserialisé.";
                return null;
            }            RehydrateGroupInfo(roster);            return roster;
        }
        catch (Exception ex)
        {
            errorMessage = $"Erreur de lecture: {ex.Message}";
            return null;
        }
    }

    public void SaveRoster(string sessionName, string courseName, string workName, List<GroupModel> groups)
    {
        var filePath = GetRosterFilePath(sessionName, courseName, workName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var roster = new RosterModel { Groups = groups };
        var json = JsonSerializer.Serialize(roster, JsonOptions);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }

    private static void RehydrateGroupInfo(RosterModel roster)
    {
        foreach (var group in roster.Groups)
        {
            foreach (var student in group.Students)
            {
                student.Group = group.DisplayName;
                student.GroupCode = group.GroupCode;
            }
        }
    }

    public void ImportCsv(string sessionName, string courseName, string workName, string csvFilePath, string? targetGroupCode = null)
    {
        if (!File.Exists(csvFilePath))
            throw new FileNotFoundException("Le fichier source n'existe pas.", csvFilePath);

        var students = ParseCsvFile(csvFilePath, out string errorMessage);
        if (students == null)
            throw new InvalidOperationException($"Format CSV invalide: {errorMessage}");

        var roster = RosterExists(sessionName, courseName, workName)
            ? (LoadRoster(sessionName, courseName, workName, out _) ?? new RosterModel())
            : new RosterModel();

        string groupCode;
        if (targetGroupCode == null)
        {
            // Comportement automatique : détecter depuis le nom de fichier
            groupCode = ExtractGroupCodeFromFileName(Path.GetFileName(csvFilePath));
            if (string.IsNullOrEmpty(groupCode))
                groupCode = GenerateNextGroupCode(roster.Groups);
        }
        else if (targetGroupCode == string.Empty)
        {
            // Forcer la création d'un nouveau groupe
            groupCode = GenerateNextGroupCode(roster.Groups);
        }
        else
        {
            // Utiliser le groupe existant spécifié
            groupCode = targetGroupCode;
        }

        var displayName = BuildDisplayName(groupCode);

        var existingGroup = roster.Groups.FirstOrDefault(g => g.GroupCode == groupCode);
        if (existingGroup != null)
        {
            existingGroup.Students = students;
            existingGroup.DisplayName = displayName;
        }
        else
        {
            roster.Groups.Add(new GroupModel
            {
                GroupCode = groupCode,
                DisplayName = displayName,
                Students = students
            });
            roster.Groups = roster.Groups.OrderBy(g => g.GroupCode).ToList();
        }

        SaveRoster(sessionName, courseName, workName, roster.Groups);
    }

    public bool ValidateCsvFormat(string filePath, out string errorMessage)
    {
        return ParseCsvFile(filePath, out errorMessage) != null;
    }

    public void CopyRoster(string sessionName, string courseName, string sourceWorkName, string destWorkName)
    {
        var sourcePath = GetRosterFilePath(sessionName, courseName, sourceWorkName);
        var destPath = GetRosterFilePath(sessionName, courseName, destWorkName);

        if (!File.Exists(sourcePath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        File.Copy(sourcePath, destPath, overwrite: true);
    }

    private List<StudentModel>? ParseCsvFile(string filePath, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (!File.Exists(filePath))
        {
            errorMessage = "Le fichier CSV n'existe pas.";
            return null;
        }

        try
        {
            var encoding = DetectCsvEncoding(filePath);
            return TryParseCsv(filePath, encoding, out errorMessage);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return null;
        }
    }

    private static Encoding DetectCsvEncoding(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        try
        {
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
            return Encoding.UTF8;
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(1252);
        }
    }

    private List<StudentModel>? TryParseCsv(string filePath, Encoding encoding, out string errorMessage)
    {
        errorMessage = string.Empty;
        using var parser = new TextFieldParser(filePath, encoding);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",", ";");
        parser.HasFieldsEnclosedInQuotes = true;

        if (parser.EndOfData)
        {
            errorMessage = "Le fichier CSV est vide.";
            return null;
        }

        var headers = parser.ReadFields();
        if (headers == null || headers.Length < 3)
        {
            errorMessage = "Le fichier CSV doit contenir au moins 3 colonnes: DA, Prénom, Nom.";
            return null;
        }

        int daIndex = -1, firstNameIndex = -1, lastNameIndex = -1, teamIndex = -1;
        for (int i = 0; i < headers.Length; i++)
        {
            var h = headers[i].ToLowerInvariant();
            if (h.Contains("da") && daIndex == -1) daIndex = i;
            if ((h.Contains("prénom") || h.Contains("prenom") || h.Contains("firstname")) && firstNameIndex == -1) firstNameIndex = i;
            if (h.Contains("nom") && !h.Contains("prénom") && !h.Contains("prenom") && lastNameIndex == -1) lastNameIndex = i;
            if ((h.Contains("Équipe") || h.Contains("equipe") || h.Contains("team")) && teamIndex == -1) teamIndex = i;
        }
        if (daIndex == -1) daIndex = 0;
        if (firstNameIndex == -1) firstNameIndex = 2;
        if (lastNameIndex == -1) lastNameIndex = 1;

        var students = new List<StudentModel>();
        while (!parser.EndOfData)
        {
            var values = parser.ReadFields();
            if (values == null || values.Length <= Math.Max(daIndex, Math.Max(firstNameIndex, lastNameIndex)))
                continue;

            students.Add(new StudentModel
            {
                Da = CleanExcelFormula(values[daIndex]),
                FirstName = CleanExcelFormula(values[firstNameIndex]),
                LastName = CleanExcelFormula(values[lastNameIndex]),
                Team = teamIndex >= 0 && int.TryParse(CleanExcelFormula(values[teamIndex]), out int team) ? team : 0
            });
        }

        return students;
    }

    private static string CleanExcelFormula(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        if (input.StartsWith("=\"") && input.EndsWith("\"")) return input[2..^1];
        if (input.StartsWith("=\"")) return input[2..];
        if (input.StartsWith('"') && input.EndsWith('"')) return input[1..^1];
        return input;
    }

    public string DetectGroupCodeInFileName(string fileName) => ExtractGroupCodeFromFileName(fileName);

    public string BuildGroupDisplayName(string groupCode) => BuildDisplayName(groupCode);

    private static string ExtractGroupCodeFromFileName(string fileName)
    {
        var match = Regex.Match(fileName, @"_(gr\d{5})_?", RegexOptions.IgnoreCase);
        if (match.Success) return match.Groups[1].Value.ToLower();
        var match2 = Regex.Match(fileName, @"\b(gr\d{5})\b", RegexOptions.IgnoreCase);
        return match2.Success ? match2.Groups[1].Value.ToLower() : string.Empty;
    }

    private static string GenerateNextGroupCode(List<GroupModel> groups)
    {
        int maxNumber = 0;
        foreach (var group in groups)
        {
            var match = Regex.Match(group.GroupCode, @"gr(\d{5})");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int n))
                maxNumber = Math.Max(maxNumber, n);
        }
        return $"gr{(maxNumber + 1):D5}";
    }

    private static string BuildDisplayName(string groupCode)
    {
        var match = Regex.Match(groupCode, @"gr(\d+)", RegexOptions.IgnoreCase);
        int number = match.Success && int.TryParse(match.Groups[1].Value, out int n) ? n : 1;
        return $"Gr. {number} ({groupCode})";
    }
}
