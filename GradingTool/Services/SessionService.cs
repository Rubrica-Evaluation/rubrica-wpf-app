using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace GradingTool.Services;

public class SessionService : ISessionService
{
    private readonly ISessionsRootService _sessionsRootService;

    public SessionService(ISessionsRootService sessionsRootService)
    {
        _sessionsRootService = sessionsRootService;
    }

    public IEnumerable<string> GetSessions()
    {
        var rootPath = _sessionsRootService.GetSessionsRootPath();
        if (rootPath == null || !Directory.Exists(rootPath))
        {
            return Enumerable.Empty<string>();
        }

        var directories = Directory.GetDirectories(rootPath);
        return directories
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .OrderBy(name => name, StringComparer.CurrentCulture)
            .ToList()!;
    }

    public void CreateSession(string sessionName)
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

        var sessionPath = Path.Combine(rootPath, sessionName);
        if (Directory.Exists(sessionPath))
        {
            throw new InvalidOperationException($"La session '{sessionName}' existe déjà.");
        }

        Directory.CreateDirectory(sessionPath);
    }

    public void DeleteSession(string sessionName)
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

        var sessionPath = Path.Combine(rootPath, sessionName);
        if (!Directory.Exists(sessionPath))
        {
            throw new InvalidOperationException($"La session '{sessionName}' n'existe pas.");
        }

        try
        {
            // Envoyer le dossier à la corbeille Windows
            FileSystem.DeleteDirectory(sessionPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException(
                $"Accès refusé au dossier '{sessionName}'.\n\n" +
                "Vérifiez que :\n" +
                "- Le dossier n'est pas ouvert dans l'explorateur Windows\n" +
                "- Aucun fichier n'est utilisé par une autre application\n" +
                "- Vous avez les permissions nécessaires");
        }
        catch (IOException ex)
        {
            throw new IOException(
                $"Impossible de supprimer '{sessionName}'.\n\n" +
                $"Le dossier ou un fichier est peut-être utilisé par une autre application.\n\n" +
                $"Détails: {ex.Message}");
        }
    }

    private void RemoveReadOnlyAttributes(string path)
    {
        var directoryInfo = new DirectoryInfo(path);

        // Retirer les attributs du dossier lui-même
        if ((directoryInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
        {
            directoryInfo.Attributes &= ~FileAttributes.ReadOnly;
        }

        // Retirer les attributs de tous les fichiers
        foreach (var file in directoryInfo.GetFiles("*", System.IO.SearchOption.AllDirectories))
        {
            if ((file.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                file.Attributes &= ~FileAttributes.ReadOnly;
            }
        }

        // Retirer les attributs de tous les sous-dossiers
        foreach (var dir in directoryInfo.GetDirectories("*", System.IO.SearchOption.AllDirectories))
        {
            if ((dir.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                dir.Attributes &= ~FileAttributes.ReadOnly;
            }
        }
    }

    public void RenameSession(string oldName, string newName)
    {
        var rootPath = _sessionsRootService.GetSessionsRootPath();
        if (rootPath == null)
        {
            throw new InvalidOperationException("Le dossier racine des sessions n'est pas configuré.");
        }

        if (string.IsNullOrWhiteSpace(oldName))
        {
            throw new ArgumentException("Le nom de la session actuelle ne peut pas être vide.", nameof(oldName));
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("Le nouveau nom de la session ne peut pas être vide.", nameof(newName));
        }

        var oldPath = Path.Combine(rootPath, oldName);
        if (!Directory.Exists(oldPath))
        {
            throw new InvalidOperationException($"La session '{oldName}' n'existe pas.");
        }

        var newPath = Path.Combine(rootPath, newName);
        if (Directory.Exists(newPath))
        {
            throw new InvalidOperationException($"La session '{newName}' existe déjà.");
        }

        Directory.Move(oldPath, newPath);
    }

    public bool HasSubdirectories(string sessionName)
    {
        var rootPath = _sessionsRootService.GetSessionsRootPath();
        if (rootPath == null)
        {
            return false;
        }

        var sessionPath = Path.Combine(rootPath, sessionName);
        if (!Directory.Exists(sessionPath))
        {
            return false;
        }

        return Directory.GetDirectories(sessionPath).Length > 0;
    }
}
