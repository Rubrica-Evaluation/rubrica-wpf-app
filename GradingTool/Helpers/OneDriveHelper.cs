using System.Diagnostics;
using System.IO;

namespace GradingTool.Helpers;

/// <summary>
/// Vérifie si un chemin se trouve dans OneDrive et si le processus OneDrive est actif.
/// Permet d'avertir l'utilisateur d'un risque de perte de données.
/// </summary>
public static class OneDriveHelper
{
    private static readonly string[] OneDriveEnvironmentVariables =
    [
        "OneDrive",
        "OneDriveConsumer",
        "OneDriveCommercial"
    ];

    public static bool IsPathInOneDrive(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var normalizedPath = Path.GetFullPath(path);

        foreach (var envVar in OneDriveEnvironmentVariables)
        {
            var oneDrivePath = Environment.GetEnvironmentVariable(envVar);
            if (string.IsNullOrEmpty(oneDrivePath))
                continue;

            var normalizedOneDrivePath = Path.GetFullPath(oneDrivePath);
            if (normalizedPath.StartsWith(normalizedOneDrivePath, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool IsOneDriveRunning()
    {
        return Process.GetProcessesByName("OneDrive").Length > 0;
    }

    public static bool ShouldWarnUser(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        return IsPathInOneDrive(path) && !IsOneDriveRunning();
    }
}
