namespace GradingTool.Services;

public interface IWorkService
{
    /// <summary>
    /// Gets all works (evaluations) for a specific course, sorted alphabetically.
    /// </summary>
    List<string> GetWorks(string sessionName, string courseName);

    /// <summary>
    /// Creates a new work directory with required subdirectories (rubric, roster, submissions, grading, pdf_docs).
    /// </summary>
    void CreateWork(string sessionName, string courseName, string workName);

    /// <summary>
    /// Renames an existing work directory.
    /// </summary>
    void RenameWork(string sessionName, string courseName, string oldWorkName, string newWorkName);

    /// <summary>
    /// Deletes a work directory (sends to recycle bin).
    /// </summary>
    void DeleteWork(string sessionName, string courseName, string workName);

    /// <summary>
    /// Gets the full path to a work directory.
    /// </summary>
    string GetWorkPath(string sessionName, string courseName, string workName);

    /// <summary>
    /// Gets the paths to all subdirectories of a work (rubric, roster, submissions, grading, pdf_docs).
    /// </summary>
    Dictionary<string, string> GetWorkSubdirectories(string sessionName, string courseName, string workName);

    /// <summary>
    /// Verifies if all required subdirectories exist for a work.
    /// Returns a list of missing subdirectory names, or empty list if all exist.
    /// </summary>
    List<string> VerifyStructure(string sessionName, string courseName, string workName);

    /// <summary>
    /// Ensures all required subdirectories exist for a work by creating missing ones.
    /// </summary>
    void EnsureStructure(string sessionName, string courseName, string workName);
}
