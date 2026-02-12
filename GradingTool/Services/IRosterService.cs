using GradingTool.Models;

namespace GradingTool.Services;

public interface IRosterService
{
    /// <summary>
    /// Checks if a roster CSV file exists in the specified evaluation's roster folder
    /// </summary>
    bool RosterExists(string sessionName, string courseName, string workName);

    /// <summary>
    /// Gets the path to the roster CSV file for the specified evaluation
    /// </summary>
    string GetRosterPath(string sessionName, string courseName, string workName);

    /// <summary>
    /// Loads and parses the roster CSV file
    /// </summary>
    /// <returns>The loaded roster or null if parsing fails</returns>
    RosterModel? LoadRoster(string sessionName, string courseName, string workName, out string errorMessage);

    /// <summary>
    /// Validates that the CSV file has the expected format
    /// </summary>
    bool ValidateCsvFormat(string filePath, out string errorMessage);

    /// <summary>
    /// Imports a CSV file to the roster folder
    /// </summary>
    void ImportRoster(string sessionName, string courseName, string workName, string sourceFilePath);

    /// <summary>
    /// Checks if a file with the given name already exists in the roster folder
    /// </summary>
    bool FileExistsInRosterFolder(string sessionName, string courseName, string workName, string fileName);

    /// <summary>
    /// Creates and saves a template CSV file to the specified location
    /// </summary>
    void SaveRosterTemplate(string destinationFilePath);

    /// <summary>
    /// Detects groups from roster file names (gr0000X pattern)
    /// </summary>
    List<GroupModel> DetectGroups(string sessionName, string courseName, string workName);

    /// <summary>
    /// Deletes a specific roster file by file path
    /// </summary>
    void DeleteRosterFile(string filePath);
}