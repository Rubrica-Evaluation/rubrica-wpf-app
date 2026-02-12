namespace GradingTool.Services;

public interface IConfigurationService
{
    string? LoadSessionsRootPath();
    void SaveSessionsRootPath(string path);
    string? LoadSelectedSession();
    void SaveSelectedSession(string? sessionName);
    string? LoadSelectedCourse();
    void SaveSelectedCourse(string? courseName);
    string? LoadSelectedWork();
    void SaveSelectedWork(string? workName);
}
