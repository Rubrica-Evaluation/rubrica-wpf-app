namespace GradingTool.Services;

public interface ISessionsRootService
{
    string? GetSessionsRootPath();
    bool IsConfigured();
    void SetupSessionsRoot(string basePath, bool createEvaluationAppFolder);
}
