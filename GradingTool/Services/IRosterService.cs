using GradingTool.Models;

namespace GradingTool.Services;

public interface IRosterService
{
    /// <summary>
    /// Checks if roster.json exists for the specified evaluation
    /// </summary>
    bool RosterExists(string sessionName, string courseName, string workName);

    /// <summary>
    /// Loads and deserializes roster.json
    /// </summary>
    RosterModel? LoadRoster(string sessionName, string courseName, string workName, out string errorMessage);

    /// <summary>
    /// Serializes and overwrites roster.json with the given groups
    /// </summary>
    void SaveRoster(string sessionName, string courseName, string workName, List<GroupModel> groups);

    /// <summary>
    /// Parses a CSV file and merges its students into roster.json.
    /// The group code is detected from the filename (gr00001 pattern), or the next available code is used.
    /// If targetGroupCode is provided: "" = force new group, non-empty = use that existing group.
    /// </summary>
    void ImportCsv(string sessionName, string courseName, string workName, string csvFilePath, string? targetGroupCode = null);

    /// <summary>
    /// Extracts the group code (e.g. "gr00001") from a CSV filename if present, or returns empty string.
    /// </summary>
    string DetectGroupCodeInFileName(string fileName);

    /// <summary>
    /// Retourne le nom d'affichage canonique pour un code de groupe (ex: "gr00001" → "Gr. 1 (gr00001)").
    /// </summary>
    string BuildGroupDisplayName(string groupCode);

    /// <summary>
    /// Validates that the CSV file has the expected format
    /// </summary>
    bool ValidateCsvFormat(string filePath, out string errorMessage);

    /// <summary>
    /// Copies roster.json from a source evaluation to a destination evaluation
    /// </summary>
    void CopyRoster(string sessionName, string courseName, string sourceWorkName, string destWorkName);
}
