using System.IO;
using GradingTool.Helpers;

namespace GradingTool.Tests.Helpers;

public class OneDriveHelperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsPathInOneDrive_NullOuVide_RetourneFalse(string? path)
    {
        Assert.False(OneDriveHelper.IsPathInOneDrive(path!));
    }

    [Fact]
    public void IsPathInOneDrive_CheminHorsOneDrive_RetourneFalse()
    {
        // Utiliser un chemin qui ne peut pas être dans OneDrive
        var path = Path.Combine(Path.GetTempPath(), "TestFolder", "data");
        
        // Vérifier que le chemin temp n'est pas dans OneDrive
        var oneDrivePath = Environment.GetEnvironmentVariable("OneDrive");
        if (oneDrivePath != null && path.StartsWith(oneDrivePath, StringComparison.OrdinalIgnoreCase))
            return; // Skip si le dossier temp est dans OneDrive (improbable)

        Assert.False(OneDriveHelper.IsPathInOneDrive(path));
    }

    [Fact]
    public void IsPathInOneDrive_CheminDansOneDrive_RetourneTrue()
    {
        var oneDrivePath = Environment.GetEnvironmentVariable("OneDrive");
        if (string.IsNullOrEmpty(oneDrivePath))
            return; // Skip si OneDrive n'est pas configuré sur cette machine

        var testPath = Path.Combine(oneDrivePath, "Documents", "Evaluation-App");
        Assert.True(OneDriveHelper.IsPathInOneDrive(testPath));
    }

    [Fact]
    public void IsOneDriveRunning_RetourneUnBooleen()
    {
        // Vérifier que la méthode ne lève pas d'exception
        var result = OneDriveHelper.IsOneDriveRunning();
        Assert.IsType<bool>(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShouldWarnUser_CheminNullOuVide_RetourneFalse(string? path)
    {
        Assert.False(OneDriveHelper.ShouldWarnUser(path));
    }

    [Fact]
    public void ShouldWarnUser_CheminHorsOneDrive_RetourneFalse()
    {
        var path = Path.Combine(Path.GetTempPath(), "TestFolder");
        
        var oneDrivePath = Environment.GetEnvironmentVariable("OneDrive");
        if (oneDrivePath != null && path.StartsWith(oneDrivePath, StringComparison.OrdinalIgnoreCase))
            return;

        Assert.False(OneDriveHelper.ShouldWarnUser(path));
    }
}
