namespace GradingTool.Models;

public record BackupInfo(string FilePath, DateTime CreatedAt)
{
    public string DisplayName => CreatedAt.ToString("yyyy-MM-dd  HH:mm:ss");
}
