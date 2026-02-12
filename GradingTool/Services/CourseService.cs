using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace GradingTool.Services;

public class CourseService : ICourseService
{
    private readonly ISessionsRootService _sessionsRootService;

    public CourseService(ISessionsRootService sessionsRootService)
    {
        _sessionsRootService = sessionsRootService;
    }

    public IEnumerable<string> GetCourses(string sessionName)
    {
        var rootPath = _sessionsRootService.GetSessionsRootPath();
        if (rootPath == null || string.IsNullOrWhiteSpace(sessionName))
        {
            return Enumerable.Empty<string>();
        }

        var sessionPath = Path.Combine(rootPath, sessionName);
        if (!Directory.Exists(sessionPath))
        {
            return Enumerable.Empty<string>();
        }

        var directories = Directory.GetDirectories(sessionPath);
        return directories
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .OrderBy(name => name, StringComparer.CurrentCulture)
            .ToList()!;
    }

    public void CreateCourse(string sessionName, string courseName)
    {
        var rootPath = _sessionsRootService.GetSessionsRootPath();
        if (rootPath == null)
        {
            throw new InvalidOperationException("Le dossier racine des sessions n'est pas configuré.");
        }

        if (string.IsNullOrWhiteSpace(sessionName))
        {
            throw new ArgumentException("Le nom de la session ne peut pas être vide.", nameof(sessionName));
        }

        if (string.IsNullOrWhiteSpace(courseName))
        {
            throw new ArgumentException("Le nom du cours ne peut pas être vide.", nameof(courseName));
        }

        var sessionPath = Path.Combine(rootPath, sessionName);
        if (!Directory.Exists(sessionPath))
        {
            throw new InvalidOperationException($"La session '{sessionName}' n'existe pas.");
        }

        var coursePath = Path.Combine(sessionPath, courseName);
        if (Directory.Exists(coursePath))
        {
            throw new InvalidOperationException($"Le cours '{courseName}' existe déjà.");
        }

        Directory.CreateDirectory(coursePath);
    }

    public void DeleteCourse(string sessionName, string courseName)
    {
        var rootPath = _sessionsRootService.GetSessionsRootPath();
        if (rootPath == null)
        {
            throw new InvalidOperationException("Le dossier racine des sessions n'est pas configuré.");
        }

        if (string.IsNullOrWhiteSpace(sessionName))
        {
            throw new ArgumentException("Le nom de la session ne peut pas être vide.", nameof(sessionName));
        }

        if (string.IsNullOrWhiteSpace(courseName))
        {
            throw new ArgumentException("Le nom du cours ne peut pas être vide.", nameof(courseName));
        }

        var sessionPath = Path.Combine(rootPath, sessionName);
        if (!Directory.Exists(sessionPath))
        {
            throw new InvalidOperationException($"La session '{sessionName}' n'existe pas.");
        }

        var coursePath = Path.Combine(sessionPath, courseName);
        if (!Directory.Exists(coursePath))
        {
            throw new InvalidOperationException($"Le cours '{courseName}' n'existe pas.");
        }

        try
        {
            // Envoyer le dossier à la corbeille Windows
            FileSystem.DeleteDirectory(coursePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException(
                $"Accès refusé au dossier '{courseName}'.\n\n" +
                "Vérifiez que :\n" +
                "- Le dossier n'est pas ouvert dans l'explorateur Windows\n" +
                "- Aucun fichier n'est utilisé par une autre application\n" +
                "- Vous avez les permissions nécessaires");
        }
        catch (IOException ex)
        {
            throw new IOException(
                $"Impossible de supprimer '{courseName}'.\n\n" +
                $"Le dossier ou un fichier est peut-être utilisé par une autre application.\n\n" +
                $"Détails: {ex.Message}");
        }
    }

    public void RenameCourse(string sessionName, string oldName, string newName)
    {
        var rootPath = _sessionsRootService.GetSessionsRootPath();
        if (rootPath == null)
        {
            throw new InvalidOperationException("Le dossier racine des sessions n'est pas configuré.");
        }

        if (string.IsNullOrWhiteSpace(sessionName))
        {
            throw new ArgumentException("Le nom de la session ne peut pas être vide.", nameof(sessionName));
        }

        if (string.IsNullOrWhiteSpace(oldName))
        {
            throw new ArgumentException("Le nom du cours actuel ne peut pas être vide.", nameof(oldName));
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("Le nouveau nom du cours ne peut pas être vide.", nameof(newName));
        }

        var sessionPath = Path.Combine(rootPath, sessionName);
        if (!Directory.Exists(sessionPath))
        {
            throw new InvalidOperationException($"La session '{sessionName}' n'existe pas.");
        }

        var oldPath = Path.Combine(sessionPath, oldName);
        if (!Directory.Exists(oldPath))
        {
            throw new InvalidOperationException($"Le cours '{oldName}' n'existe pas.");
        }

        var newPath = Path.Combine(sessionPath, newName);
        if (Directory.Exists(newPath))
        {
            throw new InvalidOperationException($"Le cours '{newName}' existe déjà.");
        }

        Directory.Move(oldPath, newPath);
    }

    public bool HasSubdirectories(string sessionName, string courseName)
    {
        var rootPath = _sessionsRootService.GetSessionsRootPath();
        if (rootPath == null || string.IsNullOrWhiteSpace(sessionName))
        {
            return false;
        }

        var sessionPath = Path.Combine(rootPath, sessionName);
        if (!Directory.Exists(sessionPath))
        {
            return false;
        }

        var coursePath = Path.Combine(sessionPath, courseName);
        if (!Directory.Exists(coursePath))
        {
            return false;
        }

        return Directory.GetDirectories(coursePath).Length > 0;
    }

    private void RemoveReadOnlyAttributes(string path)
    {
        var directoryInfo = new DirectoryInfo(path);

        if ((directoryInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
        {
            directoryInfo.Attributes &= ~FileAttributes.ReadOnly;
        }

        foreach (var file in directoryInfo.GetFiles("*", System.IO.SearchOption.AllDirectories))
        {
            if ((file.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                file.Attributes &= ~FileAttributes.ReadOnly;
            }
        }

        foreach (var dir in directoryInfo.GetDirectories("*", System.IO.SearchOption.AllDirectories))
        {
            if ((dir.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                dir.Attributes &= ~FileAttributes.ReadOnly;
            }
        }
    }
}
