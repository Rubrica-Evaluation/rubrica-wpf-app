using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace GradingTool.Services;

public class ConfigurationService : IConfigurationService
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Evaluation-App");
    
    private static readonly string ConfigFilePath = Path.Combine(
        AppDataFolder, 
        "app-config.json");

    public string? LoadSessionsRootPath()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                
                if (config?.SessionsRootPath != null && Directory.Exists(config.SessionsRootPath))
                {
                    return config.SessionsRootPath;
                }
            }
        }
        catch
        {
            // Ignorer les erreurs de chargement
        }

        return null;
    }

    public string? LoadSelectedSession()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                return config?.SelectedSession;
            }
        }
        catch
        {
            // Ignorer les erreurs de chargement
        }

        return null;
    }

    public void SaveSelectedSession(string? sessionName)
    {
        try
        {
            // Charger la config existante ou créer une nouvelle
            AppConfig config;
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            else
            {
                config = new AppConfig();
            }

            config.SelectedSession = sessionName;

            // Créer le dossier Evaluation-App s'il n'existe pas
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }

            var configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(ConfigFilePath, configJson, Encoding.UTF8);
        }
        catch
        {
            // Ignorer les erreurs de sauvegarde
        }
    }

    public string? LoadSelectedCourse()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                return config?.SelectedCourse;
            }
        }
        catch
        {
            // Ignorer les erreurs de chargement
        }

        return null;
    }

    public void SaveSelectedCourse(string? courseName)
    {
        try
        {
            // Charger la config existante ou créer une nouvelle
            AppConfig config;
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            else
            {
                config = new AppConfig();
            }

            config.SelectedCourse = courseName;

            // Créer le dossier Evaluation-App s'il n'existe pas
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }

            var configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(ConfigFilePath, configJson, Encoding.UTF8);
        }
        catch
        {
            // Ignorer les erreurs de sauvegarde
        }
    }

    public string? LoadSelectedWork()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                return config?.SelectedWork;
            }
        }
        catch
        {
            // Ignorer les erreurs de chargement
        }

        return null;
    }

    public void SaveSelectedWork(string? workName)
    {
        try
        {
            // Charger la config existante ou créer une nouvelle
            AppConfig config;
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            else
            {
                config = new AppConfig();
            }

            config.SelectedWork = workName;

            // Créer le dossier Evaluation-App s'il n'existe pas
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }

            var configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(ConfigFilePath, configJson, Encoding.UTF8);
        }
        catch
        {
            // Ignorer les erreurs de sauvegarde
        }
    }

    public void SaveSessionsRootPath(string path)
    {
        try
        {
            // Charger la config existante ou créer une nouvelle
            AppConfig config;
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            else
            {
                config = new AppConfig();
            }

            config.SessionsRootPath = path;

            // Créer le dossier Evaluation-App s'il n'existe pas
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }
            var configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(ConfigFilePath, configJson, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erreur lors de la sauvegarde de la configuration: {ex.Message}", ex);
        }
    }

    private class AppConfig
    {
        public string? SessionsRootPath { get; set; }
        public string? SelectedSession { get; set; }
        public string? SelectedCourse { get; set; }
        public string? SelectedWork { get; set; }
    }
}
