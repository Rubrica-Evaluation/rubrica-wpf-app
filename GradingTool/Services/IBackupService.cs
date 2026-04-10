using GradingTool.Models;

namespace GradingTool.Services;

public interface IBackupService
{
    Task CreateBackupAsync();
    IReadOnlyList<BackupInfo> GetAvailableBackups();
    Task<bool> RestoreBackupAsync(string zipPath);
}
