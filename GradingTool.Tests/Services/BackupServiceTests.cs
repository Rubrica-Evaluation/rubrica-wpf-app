using GradingTool.Services;
using NSubstitute;
using System.IO;
using System.IO.Compression;

namespace GradingTool.Tests.Services;

public class BackupServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _evaluationAppPath;
    private readonly string _sessionsPath;
    private readonly ISessionsRootService _sessionsRootService;
    private readonly IConfigurationService _configurationService;
    private readonly BackupService _sut;

    public BackupServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"BackupTests_{Guid.NewGuid():N}");
        _evaluationAppPath = Path.Combine(_tempRoot, "Evaluation-App");
        _sessionsPath = Path.Combine(_evaluationAppPath, "sessions");
        Directory.CreateDirectory(_sessionsPath);

        _sessionsRootService = Substitute.For<ISessionsRootService>();
        _sessionsRootService.GetSessionsRootPath().Returns(_sessionsPath);

        _configurationService = Substitute.For<IConfigurationService>();
        _configurationService.LoadBackupEnabled().Returns(true);

        _sut = new BackupService(_sessionsRootService, _configurationService);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    [Fact]
    public async Task CreateBackupAsync_BackupDisabled_CreatesNoZip()
    {
        _configurationService.LoadBackupEnabled().Returns(false);
        File.WriteAllText(Path.Combine(_sessionsPath, "data.json"), "{}");

        await _sut.CreateBackupAsync();

        var backupFolder = _evaluationAppPath + "_backups";
        Assert.False(Directory.Exists(backupFolder));
    }

    [Fact]
    public async Task CreateBackupAsync_BackupEnabled_CreatesZipInBackupFolder()
    {
        File.WriteAllText(Path.Combine(_sessionsPath, "data.json"), "{}");

        await _sut.CreateBackupAsync();

        var backupFolder = _evaluationAppPath + "_backups";
        var zips = Directory.GetFiles(backupFolder, "backup_*.zip");
        Assert.Single(zips);
    }

    [Fact]
    public async Task CreateBackupAsync_BackupEnabled_ZipContainsSessionFiles()
    {
        File.WriteAllText(Path.Combine(_sessionsPath, "rubric.json"), "{\"key\":\"value\"}");

        await _sut.CreateBackupAsync();

        var backupFolder = _evaluationAppPath + "_backups";
        var zipPath = Directory.GetFiles(backupFolder, "backup_*.zip").Single();

        using var archive = ZipFile.OpenRead(zipPath);
        var entryNames = archive.Entries.Select(e => e.FullName).ToList();
        Assert.Contains(entryNames, e => e.Contains("rubric.json"));
    }

    [Fact]
    public async Task CreateBackupAsync_BackupEnabled_ExcludesSubmissionsFolder()
    {
        var submissionsPath = Path.Combine(_sessionsPath, "tp1", "submissions");
        Directory.CreateDirectory(submissionsPath);
        File.WriteAllText(Path.Combine(submissionsPath, "student.pdf"), "pdf content");

        await _sut.CreateBackupAsync();

        var backupFolder = _evaluationAppPath + "_backups";
        var zipPath = Directory.GetFiles(backupFolder, "backup_*.zip").Single();

        using var archive = ZipFile.OpenRead(zipPath);
        var entryNames = archive.Entries.Select(e => e.FullName).ToList();
        Assert.DoesNotContain(entryNames, e => e.Contains("submissions"));
    }

    [Fact]
    public async Task CreateBackupAsync_BackupEnabled_ExcludesPdfDocsFolder()
    {
        var pdfDocsPath = Path.Combine(_sessionsPath, "tp1", "pdf_docs");
        Directory.CreateDirectory(pdfDocsPath);
        File.WriteAllText(Path.Combine(pdfDocsPath, "export.pdf"), "pdf content");

        await _sut.CreateBackupAsync();

        var backupFolder = _evaluationAppPath + "_backups";
        var zipPath = Directory.GetFiles(backupFolder, "backup_*.zip").Single();

        using var archive = ZipFile.OpenRead(zipPath);
        var entryNames = archive.Entries.Select(e => e.FullName).ToList();
        Assert.DoesNotContain(entryNames, e => e.Contains("pdf_docs"));
    }

    [Fact]
    public async Task CreateBackupAsync_MoreThanFiveBackups_PurgesOldest()
    {
        var backupFolder = _evaluationAppPath + "_backups";
        Directory.CreateDirectory(backupFolder);

        // Pré-créer 5 zips anciens avec des timestamps différents
        for (int i = 1; i <= 5; i++)
        {
            var fakePath = Path.Combine(backupFolder, $"backup_2020-01-0{i}_00-00-00.zip");
            File.WriteAllBytes(fakePath, []);
        }

        await _sut.CreateBackupAsync();

        var zips = Directory.GetFiles(backupFolder, "backup_*.zip");
        Assert.Equal(5, zips.Length);
    }

    [Fact]
    public void GetAvailableBackups_NoBackupFolder_ReturnsEmptyList()
    {
        var backups = _sut.GetAvailableBackups();

        Assert.Empty(backups);
    }

    [Fact]
    public async Task GetAvailableBackups_AfterCreateBackup_ReturnsOneEntry()
    {
        await _sut.CreateBackupAsync();

        var backups = _sut.GetAvailableBackups();

        Assert.Single(backups);
    }

    [Fact]
    public void GetAvailableBackups_MultipleBackups_ReturnsSortedByDateDescending()
    {
        var backupFolder = _evaluationAppPath + "_backups";
        Directory.CreateDirectory(backupFolder);

        var older = Path.Combine(backupFolder, "backup_2025-01-01_00-00-00.zip");
        var newer = Path.Combine(backupFolder, "backup_2026-01-01_00-00-00.zip");
        File.WriteAllBytes(older, []);
        File.WriteAllBytes(newer, []);
        File.SetLastWriteTime(older, new DateTime(2025, 1, 1));
        File.SetLastWriteTime(newer, new DateTime(2026, 1, 1));

        var backups = _sut.GetAvailableBackups();

        Assert.Equal(2, backups.Count);
        Assert.True(backups[0].CreatedAt > backups[1].CreatedAt);
    }

    [Fact]
    public async Task RestoreBackupAsync_ValidZip_ReplacesEvaluationAppFolder()
    {
        // Créer un backup avec un fichier spécifique
        File.WriteAllText(Path.Combine(_sessionsPath, "original.json"), "original");
        await _sut.CreateBackupAsync();
        var backupPath = _sut.GetAvailableBackups().Single().FilePath;

        // Modifier le contenu actuel
        File.WriteAllText(Path.Combine(_sessionsPath, "modified.json"), "modified");
        File.Delete(Path.Combine(_sessionsPath, "original.json"));

        var result = await _sut.RestoreBackupAsync(backupPath);

        Assert.True(result);
        Assert.True(File.Exists(Path.Combine(_sessionsPath, "original.json")));
        Assert.False(File.Exists(Path.Combine(_sessionsPath, "modified.json")));
    }

    [Fact]
    public async Task RestoreBackupAsync_InvalidZipPath_ReturnsFalse()
    {
        var result = await _sut.RestoreBackupAsync(@"C:\nonexistent\backup.zip");

        Assert.False(result);
    }

    [Fact]
    public void GetAvailableBackups_SessionsRootNotConfigured_ReturnsEmptyList()
    {
        _sessionsRootService.GetSessionsRootPath().Returns((string?)null);

        var backups = _sut.GetAvailableBackups();

        Assert.Empty(backups);
    }
}
