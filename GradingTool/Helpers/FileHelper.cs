using System.IO;
using System.Text;

namespace GradingTool.Helpers;

/// <summary>
/// Écriture atomique : écrit dans un fichier temporaire puis renomme,
/// pour éviter la corruption en cas de sync OneDrive ou de crash.
/// </summary>
public static class FileHelper
{
    public static async Task WriteAllTextAtomicAsync(string filePath, string content, Encoding? encoding = null)
    {
        var tempPath = filePath + ".tmp";

        await File.WriteAllTextAsync(tempPath, content, encoding ?? Encoding.UTF8);
        try
        {
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch
        {
            try { File.Delete(tempPath); } catch { }
            throw;
        }
    }

    public static void WriteAllTextAtomic(string filePath, string content, Encoding? encoding = null)
    {
        var tempPath = filePath + ".tmp";

        File.WriteAllText(tempPath, content, encoding ?? Encoding.UTF8);
        try
        {
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch
        {
            try { File.Delete(tempPath); } catch { }
            throw;
        }
    }
}
