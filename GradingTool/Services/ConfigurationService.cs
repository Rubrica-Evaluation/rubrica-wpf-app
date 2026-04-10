using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace GradingTool.Services;

public class ConfigurationService : IConfigurationService
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
#if DEBUG
        "Rubrica-dev");
#else
        "Rubrica");
#endif
    
    private static readonly string ConfigFilePath = Path.Combine(
        AppDataFolder,
        "app-config.json");

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string? LoadSessionsRootPath()
    {
        try
        {
            var path = LoadConfig()?.SessionsRootPath;
            return path != null && Directory.Exists(path) ? path : null;
        }
        catch { return null; }
    }

    public string? LoadSelectedSession()
    {
        try { return LoadConfig()?.SelectedSession; }
        catch { return null; }
    }

    public void SaveSelectedSession(string? sessionName)
    {
        try
        {
            var config = LoadConfig() ?? new AppConfig();
            config.SelectedSession = sessionName;
            SaveConfig(config);
        }
        catch { }
    }

    public string? LoadSelectedCourse()
    {
        try { return LoadConfig()?.SelectedCourse; }
        catch { return null; }
    }

    public void SaveSelectedCourse(string? courseName)
    {
        try
        {
            var config = LoadConfig() ?? new AppConfig();
            config.SelectedCourse = courseName;
            SaveConfig(config);
        }
        catch { }
    }

    public string? LoadSelectedWork()
    {
        try { return LoadConfig()?.SelectedWork; }
        catch { return null; }
    }

    public void SaveSelectedWork(string? workName)
    {
        try
        {
            var config = LoadConfig() ?? new AppConfig();
            config.SelectedWork = workName;
            SaveConfig(config);
        }
        catch { }
    }

    public void SaveSessionsRootPath(string path)
    {
        try
        {
            var config = LoadConfig() ?? new AppConfig();
            config.SessionsRootPath = path;
            SaveConfig(config);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erreur lors de la sauvegarde de la configuration: {ex.Message}", ex);
        }
    }

    private AppConfig? LoadConfig()
    {
        if (!File.Exists(ConfigFilePath)) return null;
        var json = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
        return JsonSerializer.Deserialize<AppConfig>(json);
    }

    private void SaveConfig(AppConfig config)
    {
        Directory.CreateDirectory(AppDataFolder);
        File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(config, _jsonOptions), Encoding.UTF8);
    }

    public string? LoadLanguage()
    {
        try { return LoadConfig()?.Language; }
        catch { return null; }
    }

    public void SaveLanguage(string language)
    {
        try
        {
            var config = LoadConfig() ?? new AppConfig();
            config.Language = language;
            SaveConfig(config);
        }
        catch { }
    }

    public bool LoadBackupEnabled()
    {
        try { return LoadConfig()?.BackupEnabled ?? true; }
        catch { return true; }
    }

    public void SaveBackupEnabled(bool enabled)
    {
        try
        {
            var config = LoadConfig() ?? new AppConfig();
            config.BackupEnabled = enabled;
            SaveConfig(config);
        }
        catch { }
    }

    public int LoadBackupIntervalMinutes()
    {
        try { return LoadConfig()?.BackupIntervalMinutes ?? 30; }
        catch { return 30; }
    }

    public void SaveBackupIntervalMinutes(int minutes)
    {
        try
        {
            var config = LoadConfig() ?? new AppConfig();
            config.BackupIntervalMinutes = minutes;
            SaveConfig(config);
        }
        catch { }
    }

    public int LoadBackupMaxCount()
    {
        try { return LoadConfig()?.BackupMaxCount ?? 10; }
        catch { return 10; }
    }

    public void SaveBackupMaxCount(int count)
    {
        try
        {
            var config = LoadConfig() ?? new AppConfig();
            config.BackupMaxCount = count;
            SaveConfig(config);
        }
        catch { }
    }

    private class AppConfig
    {
        public string? SessionsRootPath { get; set; }
        public string? SelectedSession { get; set; }
        public string? SelectedCourse { get; set; }
        public string? SelectedWork { get; set; }
        public string? Language { get; set; }
        public bool BackupEnabled { get; set; } = true;
        public int BackupIntervalMinutes { get; set; } = 30;
        public int BackupMaxCount { get; set; } = 10;
    }
}
