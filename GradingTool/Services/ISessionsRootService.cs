using GradingTool.Helpers;

namespace GradingTool.Services;

public interface ISessionsRootService
{
    static readonly string EvaluationAppFolderName = AppIdentity.AppFolderName;

    string? GetSessionsRootPath();
    bool IsConfigured();
    void SetupSessionsRoot(string basePath, bool createEvaluationAppFolder);
}
