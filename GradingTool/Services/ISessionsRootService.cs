namespace GradingTool.Services;

public interface ISessionsRootService
{
#if DEBUG
    static readonly string EvaluationAppFolderName = "Rubrica-dev";
#else
    static readonly string EvaluationAppFolderName = "Rubrica";
#endif

    string? GetSessionsRootPath();
    bool IsConfigured();
    void SetupSessionsRoot(string basePath, bool createEvaluationAppFolder);
}
