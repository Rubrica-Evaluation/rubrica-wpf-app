using GradingTool.Models;

namespace GradingTool.Services;

public interface IRubricService
{
    /// <summary>
    /// Checks if a rubric.json file exists in the specified evaluation's rubric folder
    /// </summary>
    bool RubricExists(string sessionName, string courseName, string workName);

    /// <summary>
    /// Gets the path to the rubric.json file for the specified evaluation
    /// </summary>
    string GetRubricPath(string sessionName, string courseName, string workName);

    /// <summary>
    /// Loads and validates the rubric.json file
    /// </summary>
    /// <returns>The loaded rubric or null if validation fails</returns>
    RubricModel? LoadRubric(string sessionName, string courseName, string workName, out string errorMessage);

    /// <summary>
    /// Creates an empty rubric draft for the specified evaluation
    /// </summary>
    RubricModel CreateEmptyRubric(string workName);

    /// <summary>
    /// Saves the rubric.json file for the specified evaluation
    /// </summary>
    bool SaveRubric(string sessionName, string courseName, string workName, RubricModel rubric, out string errorMessage);

    /// <summary>
    /// Validates that the rubric matches the expected format
    /// </summary>
    bool ValidateRubricFormat(RubricModel rubric, out string errorMessage);

    /// <summary>
    /// Copies a selected file to the rubric folder as rubric.json
    /// </summary>
    void ImportRubric(string sessionName, string courseName, string workName, string sourceFilePath);

    /// <summary>
    /// Loads and validates a rubric from an arbitrary file path
    /// </summary>
    RubricModel? LoadRubricFromFile(string filePath, out string errorMessage);

    /// <summary>
    /// Creates and saves a template rubric.json file to the specified location
    /// </summary>
    void SaveRubricTemplate(string destinationFilePath);
}
