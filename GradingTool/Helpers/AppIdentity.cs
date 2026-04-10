namespace GradingTool.Helpers;

/// <summary>
/// Constantes d'identité de l'application, partagées par tous les services.
/// </summary>
public static class AppIdentity
{
#if DEBUG
    public const string AppFolderName = "Rubrica-dev";
#else
    public const string AppFolderName = "Rubrica";
#endif
}
