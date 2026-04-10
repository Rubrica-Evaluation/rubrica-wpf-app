using System.IO;
using System.IO.Compression;
using GradingTool.Models;

namespace GradingTool.Services;

public class BackupService : IBackupService
{
    private static readonly HashSet<string> ExcludedFolderNames =
        new(StringComparer.OrdinalIgnoreCase) { "submissions", "pdf_docs" };

    private readonly ISessionsRootService _sessionsRootService;
    private readonly IConfigurationService _configurationService;
    private readonly System.Timers.Timer _periodicTimer;
    private volatile bool _suppressNextBackup;

    public BackupService(ISessionsRootService sessionsRootService, IConfigurationService configurationService)
    {
        _sessionsRootService = sessionsRootService;
        _configurationService = configurationService;

        var intervalMinutes = _configurationService.LoadBackupIntervalMinutes();
        _periodicTimer = new System.Timers.Timer(TimeSpan.FromMinutes(intervalMinutes).TotalMilliseconds);
        _periodicTimer.Elapsed += async (_, _) => await CreateBackupAsync();
        _periodicTimer.AutoReset = true;
        _periodicTimer.Start();
    }

    public void Dispose()
    {
        _periodicTimer.Stop();
        _periodicTimer.Dispose();
    }

    public void UpdateTimerInterval(int minutes)
    {
        _periodicTimer.Stop();
        _periodicTimer.Interval = TimeSpan.FromMinutes(minutes).TotalMilliseconds;
        _periodicTimer.Start();
    }

    public void SuppressNextBackup() => _suppressNextBackup = true;

    public async Task CreateBackupAsync()
    {
        if (_suppressNextBackup)
        {
            _suppressNextBackup = false;
            return;
        }

        if (!_configurationService.LoadBackupEnabled())
            return;

        var evaluationAppPath = ResolveEvaluationAppPath();
        if (evaluationAppPath == null)
            return;

        try
        {
            var backupFolder = evaluationAppPath + "_backups";
            Directory.CreateDirectory(backupFolder);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var zipPath = Path.Combine(backupFolder, $"backup_{timestamp}.zip");

            await Task.Run(() => CreateZip(evaluationAppPath, zipPath)).ConfigureAwait(false);

            PurgeOldBackups(backupFolder);
        }
        catch { }
    }

    public IReadOnlyList<BackupInfo> GetAvailableBackups()
    {
        var evaluationAppPath = ResolveEvaluationAppPath();
        if (evaluationAppPath == null)
            return [];

        var backupFolder = evaluationAppPath + "_backups";
        if (!Directory.Exists(backupFolder))
            return [];

        return Directory.GetFiles(backupFolder, "backup_*.zip")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTime)
            .Select(f => new BackupInfo(f.FullName, f.LastWriteTime))
            .ToList();
    }

    public async Task<bool> RestoreBackupAsync(string zipPath)
    {
        var evaluationAppPath = ResolveEvaluationAppPath();
        if (evaluationAppPath == null)
            return false;

        try
        {
            await Task.Run(() =>
            {
                var parentPath = Path.GetDirectoryName(evaluationAppPath)!;

                if (Directory.Exists(evaluationAppPath))
                {
                    RemoveReadOnlyAttributes(evaluationAppPath);
                    Directory.Delete(evaluationAppPath, recursive: true);
                }

                ZipFile.ExtractToDirectory(zipPath, parentPath);
            }).ConfigureAwait(false);

            return true;
        }
        catch { return false; }
    }

    private string? ResolveEvaluationAppPath()
    {
        var sessionsRoot = _sessionsRootService.GetSessionsRootPath();
        if (sessionsRoot == null)
            return null;

        // sessionsRoot = .../Rubrica/sessions  →  parent = .../Rubrica
        return Path.GetDirectoryName(sessionsRoot);
    }

    private static void RemoveReadOnlyAttributes(string path)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories).Prepend(path))
        {
            var attributes = File.GetAttributes(entry);
            if ((attributes & FileAttributes.ReadOnly) != 0)
                File.SetAttributes(entry, attributes & ~FileAttributes.ReadOnly);
        }
    }

    private void CreateZip(string sourceFolderPath, string destinationZipPath)
    {
        // RootPath is the parent so entries include the folder name, e.g. Rubrica/sessions/...
        var rootPath = Path.GetDirectoryName(sourceFolderPath)!;
        using var zipStream = new FileStream(destinationZipPath, FileMode.Create, FileAccess.Write);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false);

        AddDirectoryToZip(archive, sourceFolderPath, rootPath);
    }

    private void AddDirectoryToZip(ZipArchive archive, string currentPath, string rootPath)
    {
        foreach (var file in Directory.EnumerateFiles(currentPath))
        {
            var entryName = Path.GetRelativePath(rootPath, file).Replace('\\', '/');
            archive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
        }

        foreach (var directory in Directory.EnumerateDirectories(currentPath))
        {
            if (ExcludedFolderNames.Contains(Path.GetFileName(directory)))
                continue;

            AddDirectoryToZip(archive, directory, rootPath);
        }
    }

    private void PurgeOldBackups(string backupFolder)
    {
        var maxCount = _configurationService.LoadBackupMaxCount();
        var oldBackups = Directory.GetFiles(backupFolder, "backup_*.zip")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTime)
            .Skip(maxCount);

        foreach (var backup in oldBackups)
            backup.Delete();
    }
}
