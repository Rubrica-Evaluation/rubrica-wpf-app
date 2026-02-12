using GradingTool.Models;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

namespace GradingTool.Services;

public class RosterService : IRosterService
{
    private readonly ISessionsRootService _sessionsRootService;

    public RosterService(ISessionsRootService sessionsRootService)
    {
        _sessionsRootService = sessionsRootService;
    }

    public bool RosterExists(string sessionName, string courseName, string workName)
    {
        var rosterDir = GetRosterDirectory(sessionName, courseName, workName);
        return Directory.Exists(rosterDir) && Directory.GetFiles(rosterDir, "*.csv").Length > 0;
    }

    public string GetRosterPath(string sessionName, string courseName, string workName)
    {
        var rosterDir = GetRosterDirectory(sessionName, courseName, workName);
        var csvFiles = Directory.GetFiles(rosterDir, "*.csv");
        return csvFiles.Length > 0 ? csvFiles[0] : Path.Combine(rosterDir, "roster.csv");
    }

    private string GetRosterDirectory(string sessionName, string courseName, string workName)
    {
        var rootPath = _sessionsRootService.GetSessionsRootPath();
        if (string.IsNullOrEmpty(rootPath))
        {
            throw new InvalidOperationException("Le dossier racine des sessions n'est pas configuré.");
        }
        return Path.Combine(rootPath, sessionName, courseName, workName, "roster");
    }

    public RosterModel? LoadRoster(string sessionName, string courseName, string workName, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (!RosterExists(sessionName, courseName, workName))
        {
            errorMessage = "Aucun fichier CSV trouvé dans le dossier roster.";
            return null;
        }

        var rosterPath = GetRosterPath(sessionName, courseName, workName);

        try
        {
            // Extraire le code du groupe du nom du fichier CSV
            string groupCode = ExtractGroupCodeFromFileName(Path.GetFileName(rosterPath));
            
            var roster = ParseCsvFile(rosterPath, groupCode, out errorMessage);
            if (roster == null)
            {
                return null;
            }

            // Detect groups from file names
            roster.Groups = DetectGroups(sessionName, courseName, workName);

            return roster;
        }
        catch (Exception ex)
        {
            errorMessage = $"Erreur lors de la lecture du fichier CSV: {ex.Message}";
            return null;
        }
    }

    private string ExtractGroupNumber(string groupCode)
    {
        // Extraire le numéro du groupe du code (ex: "gr00001" → "00001" ou "gr001" → "001")
        if (string.IsNullOrEmpty(groupCode))
            return string.Empty;
        
        // Chercher le pattern "gr" suivi de chiffres
        var match = System.Text.RegularExpressions.Regex.Match(groupCode, @"gr(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        
        return groupCode;
    }

    private string ExtractGroupCodeFromFileName(string fileName)
    {
        // Chercher un pattern comme "gr00001" ou "groupe.001" dans le nom du fichier
        var match = System.Text.RegularExpressions.Regex.Match(fileName, @"_(gr\d+)_", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return string.Empty;
    }

    private RosterModel? ParseCsvFile(string filePath, string groupCode, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (!File.Exists(filePath))
        {
            errorMessage = "Le fichier CSV n'existe pas.";
            return null;
        }

        try
        {
            using (var parser = new TextFieldParser(filePath, Encoding.GetEncoding(1252)))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;

                // Read headers
                if (parser.EndOfData)
                {
                    errorMessage = "Le fichier CSV est vide.";
                    return null;
                }

                var headers = parser.ReadFields();
                if (headers == null || headers.Length < 3)
                {
                    errorMessage = "Le fichier CSV doit contenir au moins un en-tête avec 3 colonnes: DA, Prénom, Nom.";
                    return null;
                }

                var students = new List<StudentModel>();
                
                // Améliorer la détection des colonnes avec une correspondance plus flexible
                var daIndex = -1;
                var firstNameIndex = -1;
                var lastNameIndex = -1;
                var teamIndex = -1;
                
                for (int i = 0; i < headers.Length; i++)
                {
                    var headerLower = headers[i].ToLower();
                    // DA column
                    if (headerLower.Contains("da") && daIndex == -1)
                        daIndex = i;
                    // FirstName: chercher "prénom" ou "firstname"
                    if ((headerLower.Contains("prénom") || headerLower.Contains("firstname")) && firstNameIndex == -1)
                        firstNameIndex = i;
                    // LastName: chercher "nom" et non "prénom"
                    if (headerLower.Contains("nom") && !headerLower.Contains("prénom") && lastNameIndex == -1)
                        lastNameIndex = i;
                    // Team: chercher "équipe" ou "team"  
                    if ((headerLower.Contains("équipe") || headerLower.Contains("team")) && teamIndex == -1)
                        teamIndex = i;
                }
                
                // Fallback aux indices par défaut si colonnes pas trouvées
                if (daIndex == -1) daIndex = 0;
                if (lastNameIndex == -1) lastNameIndex = 1;  // Nom avant Prénom dans le CSV
                if (firstNameIndex == -1) firstNameIndex = 2;

                // Read data rows
                while (!parser.EndOfData)
                {
                    var values = parser.ReadFields();
                    if (values == null || values.Length <= Math.Max(daIndex, Math.Max(firstNameIndex, lastNameIndex)))
                        continue;

                    var student = new StudentModel
                    {
                        Da = CleanExcelFormula(values[daIndex]),
                        FirstName = CleanExcelFormula(values[firstNameIndex]),
                        LastName = CleanExcelFormula(values[lastNameIndex]),
                        Team = teamIndex >= 0 && int.TryParse(CleanExcelFormula(values[teamIndex]), out int team) ? team : 0,
                        Group = ExtractGroupNumber(groupCode),
                        GroupCode = groupCode
                    };

                    students.Add(student);
                }

                return new RosterModel { Students = students };
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Erreur lors de la lecture du fichier CSV: {ex.Message}";
            return null;
        }
    }

    private string CleanExcelFormula(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        // Supprimer les formules Excel du type ="valeur" ou ="valeur (avec ou sans guillemet final)
        if (input.StartsWith("=\""))
        {
            // Cas 1: ="valeur"
            if (input.EndsWith("\""))
            {
                return input.Substring(2, input.Length - 3);
            }
            // Cas 2: ="valeur (guillemet final enlevé par Trim)
            else if (input.Length > 2)
            {
                return input.Substring(2);
            }
        }
        
        // Supprimer les guillemets simples si présents
        if (input.StartsWith("\"") && input.EndsWith("\""))
        {
            return input.Substring(1, input.Length - 2);
        }
        
        return input;
    }

    public bool ValidateCsvFormat(string filePath, out string errorMessage)
    {
        string groupCode = ExtractGroupCodeFromFileName(Path.GetFileName(filePath));
        return ParseCsvFile(filePath, groupCode, out errorMessage) != null;
    }

    public void ImportRoster(string sessionName, string courseName, string workName, string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Le fichier source n'existe pas.", sourceFilePath);
        }

        // Validate the CSV format first
        if (!ValidateCsvFormat(sourceFilePath, out string errorMessage))
        {
            throw new InvalidOperationException($"Format CSV invalide: {errorMessage}");
        }

        // Copy to destination with original filename
        var destinationDir = GetRosterDirectory(sessionName, courseName, workName);
        if (!Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        var originalFileName = Path.GetFileName(sourceFilePath);
        var destinationPath = Path.Combine(destinationDir, originalFileName);
        File.Copy(sourceFilePath, destinationPath, overwrite: true);
    }

    public bool FileExistsInRosterFolder(string sessionName, string courseName, string workName, string fileName)
    {
        var rosterDir = GetRosterDirectory(sessionName, courseName, workName);
        if (!Directory.Exists(rosterDir))
        {
            return false;
        }

        var filePath = Path.Combine(rosterDir, fileName);
        return File.Exists(filePath);
    }

    public void SaveRosterTemplate(string destinationFilePath)
    {
        var template = @"DA,Prénom,Nom,Équipe
123456789,Jean,Dupont,1
987654321,Marie,Martin,1
456789123,Paul,Dubois,2
789123456,Sophie,Leroy,2
321654987,Marc,Moreau,3";

        File.WriteAllText(destinationFilePath, template, Encoding.UTF8);
    }

    public List<GroupModel> DetectGroups(string sessionName, string courseName, string workName)
    {
        var rosterDir = GetRosterDirectory(sessionName, courseName, workName);
        if (!Directory.Exists(rosterDir))
        {
            return new List<GroupModel>();
        }

        var csvFiles = Directory.GetFiles(rosterDir, "*.csv");
        var groups = new List<GroupModel>();

        foreach (var filePath in csvFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var match = Regex.Match(fileName, @"gr(\d{5})", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var groupNumber = match.Groups[1].Value;
                var groupCode = $"gr{groupNumber}";
                var displayName = $"Gr. {int.Parse(groupNumber)} ({groupCode})";

                var group = new GroupModel
                {
                    GroupCode = groupCode,
                    DisplayName = displayName,
                    FilePath = filePath
                };

                // Load students for this group
                var roster = ParseCsvFile(filePath, groupCode, out _);
                if (roster != null)
                {
                    group.Students = roster.Students;
                    foreach (var student in group.Students)
                    {
                        student.GroupCode = groupCode;
                        student.Group = displayName;
                    }
                }

                groups.Add(group);
            }
        }

        return groups.OrderBy(g => g.GroupCode).ToList();
    }

    public void DeleteRosterFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return;
        }

        // Send to recycle bin instead of permanent deletion
        FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
    }
}