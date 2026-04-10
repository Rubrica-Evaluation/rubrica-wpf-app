using System.IO;

namespace GradingTool.Services;

public class SessionsRootService : ISessionsRootService
{
    private readonly IConfigurationService _configurationService;

    public SessionsRootService(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    public string? GetSessionsRootPath()
    {
        return _configurationService.LoadSessionsRootPath();
    }

    public bool IsConfigured()
    {
        var path = GetSessionsRootPath();
        return path != null && Directory.Exists(path);
    }

    public void SetupSessionsRoot(string basePath, bool createEvaluationAppFolder)
    {
        var targetPath = basePath;

        // Créer le dossier Evaluation-App si nécessaire
        if (createEvaluationAppFolder)
        {
            targetPath = Path.Combine(basePath, ISessionsRootService.EvaluationAppFolderName);
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }
        }

        // Créer le sous-dossier sessions
        var sessionsPath = Path.Combine(targetPath, "sessions");
        if (!Directory.Exists(sessionsPath))
        {
            Directory.CreateDirectory(sessionsPath);
        }

        // Sauvegarder la configuration
        _configurationService.SaveSessionsRootPath(sessionsPath);
    }
}
