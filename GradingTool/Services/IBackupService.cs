using GradingTool.Models;

namespace GradingTool.Services;

public interface IBackupService : IDisposable
{
    Task CreateBackupAsync();
    IReadOnlyList<BackupInfo> GetAvailableBackups();
    Task<bool> RestoreBackupAsync(string zipPath);
    void UpdateTimerInterval(int minutes);
    void SuppressNextBackup();
}
