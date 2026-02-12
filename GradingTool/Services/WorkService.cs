using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace GradingTool.Services;

public class WorkService : IWorkService
{
    private readonly ISessionsRootService _sessionsRootService;
    private static readonly string[] RequiredSubdirectories = { "rubric", "roster", "submissions", "grading", "pdf_docs" };

    public WorkService(ISessionsRootService sessionsRootService)
    {
        _sessionsRootService = sessionsRootService;
    }

    public List<string> GetWorks(string sessionName, string courseName)
    {
        var root = _sessionsRootService.GetSessionsRootPath();
        if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(sessionName) || string.IsNullOrEmpty(courseName))
            return new List<string>();

        var coursePath = Path.Combine(root, sessionName, courseName);
        if (!Directory.Exists(coursePath))
            return new List<string>();

        return Directory.GetDirectories(coursePath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .OrderBy(name => name, StringComparer.CurrentCulture)
            .ToList()!;
    }

    public void CreateWork(string sessionName, string courseName, string workName)
    {
        var root = _sessionsRootService.GetSessionsRootPath();
        if (string.IsNullOrEmpty(root))
            throw new InvalidOperationException("Sessions root is not configured.");

        var workPath = Path.Combine(root, sessionName, courseName, workName);
        Directory.CreateDirectory(workPath);

        // Create required subdirectories
        foreach (var subdir in RequiredSubdirectories)
        {
            Directory.CreateDirectory(Path.Combine(workPath, subdir));
        }
    }

    public void RenameWork(string sessionName, string courseName, string oldWorkName, string newWorkName)
    {
        var root = _sessionsRootService.GetSessionsRootPath();
        if (string.IsNullOrEmpty(root))
            throw new InvalidOperationException("Sessions root is not configured.");

        var oldPath = Path.Combine(root, sessionName, courseName, oldWorkName);
        var newPath = Path.Combine(root, sessionName, courseName, newWorkName);

        if (!Directory.Exists(oldPath))
            throw new DirectoryNotFoundException($"Work '{oldWorkName}' does not exist.");

        if (Directory.Exists(newPath))
            throw new InvalidOperationException($"A work named '{newWorkName}' already exists.");

        Directory.Move(oldPath, newPath);
    }

    public void DeleteWork(string sessionName, string courseName, string workName)
    {
        var root = _sessionsRootService.GetSessionsRootPath();
        if (string.IsNullOrEmpty(root))
            throw new InvalidOperationException("Sessions root is not configured.");

        var workPath = Path.Combine(root, sessionName, courseName, workName);

        if (!Directory.Exists(workPath))
            throw new DirectoryNotFoundException($"Work '{workName}' does not exist.");

        // Remove read-only attributes recursively before deletion
        RemoveReadOnlyAttributes(workPath);

        // Send to recycle bin
        FileSystem.DeleteDirectory(workPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
    }

    public string GetWorkPath(string sessionName, string courseName, string workName)
    {
        var root = _sessionsRootService.GetSessionsRootPath();
        if (string.IsNullOrEmpty(root))
            return string.Empty;

        return Path.Combine(root, sessionName, courseName, workName);
    }

    public Dictionary<string, string> GetWorkSubdirectories(string sessionName, string courseName, string workName)
    {
        var workPath = GetWorkPath(sessionName, courseName, workName);
        var subdirectories = new Dictionary<string, string>();

        foreach (var subdir in RequiredSubdirectories)
        {
            var subdirPath = Path.Combine(workPath, subdir);
            subdirectories[subdir] = subdirPath;
        }

        return subdirectories;
    }

    public List<string> VerifyStructure(string sessionName, string courseName, string workName)
    {
        var workPath = GetWorkPath(sessionName, courseName, workName);
        if (string.IsNullOrEmpty(workPath) || !Directory.Exists(workPath))
            return new List<string>();

        var missingDirectories = new List<string>();

        foreach (var subdir in RequiredSubdirectories)
        {
            var subdirPath = Path.Combine(workPath, subdir);
            if (!Directory.Exists(subdirPath))
            {
                missingDirectories.Add(subdir);
            }
        }

        return missingDirectories;
    }

    public void EnsureStructure(string sessionName, string courseName, string workName)
    {
        var workPath = GetWorkPath(sessionName, courseName, workName);
        if (string.IsNullOrEmpty(workPath))
            throw new InvalidOperationException("Cannot determine work path.");

        foreach (var subdir in RequiredSubdirectories)
        {
            var subdirPath = Path.Combine(workPath, subdir);
            if (!Directory.Exists(subdirPath))
            {
                Directory.CreateDirectory(subdirPath);
            }
        }
    }

    private void RemoveReadOnlyAttributes(string path)
    {
        var dirInfo = new DirectoryInfo(path);
        dirInfo.Attributes &= ~FileAttributes.ReadOnly;

        foreach (var file in dirInfo.GetFiles("*", System.IO.SearchOption.AllDirectories))
        {
            file.Attributes &= ~FileAttributes.ReadOnly;
        }

        foreach (var dir in dirInfo.GetDirectories("*", System.IO.SearchOption.AllDirectories))
        {
            dir.Attributes &= ~FileAttributes.ReadOnly;
        }
    }
}
