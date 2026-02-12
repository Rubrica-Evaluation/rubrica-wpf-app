namespace GradingTool.Services;

public interface ISessionService
{
    IEnumerable<string> GetSessions();
    void CreateSession(string sessionName);
    void DeleteSession(string sessionName);
    void RenameSession(string oldName, string newName);
    bool HasSubdirectories(string sessionName);
}
