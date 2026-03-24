using GradingTool.Models;

namespace GradingTool.Services;

public interface IGridService
{
    /// <summary>
    /// Génère une grille JSON pour un étudiant spécifique
    /// </summary>
    /// <param name="student">L'étudiant pour lequel générer la grille</param>
    /// <param name="rubric">Le rubric à utiliser comme template</param>
    /// <param name="tpName">Le nom du TP (ex: "TP1")</param>
    /// <returns>La grille générée</returns>
    GridModel GenerateGrid(StudentModel student, RubricModel rubric, string tpName);

    /// <summary>
    /// Sauvegarde une grille dans le dossier grading approprié
    /// </summary>
    /// <param name="grid">La grille à sauvegarder</param>
    /// <param name="basePath">Le chemin de base de la session (ex: sessions/Hiver 2026/BD1/TP1)</param>
    /// <returns>True si sauvegardé avec succès</returns>
    Task<bool> SaveGridAsync(GridModel grid, string basePath);

    /// <summary>
    /// Vérifie si une grille existe déjà pour un étudiant
    /// </summary>
    /// <param name="student">L'étudiant</param>
    /// <param name="basePath">Le chemin de base de la session</param>
    /// <returns>True si la grille existe</returns>
    bool GridExists(StudentModel student, string basePath);

    /// <summary>
    /// Génère une grille JSON pour une équipe d'étudiants
    /// </summary>
    /// <param name="teamStudents">La liste des étudiants de l'équipe</param>
    /// <param name="rubric">Le rubric à utiliser comme template</param>
    /// <param name="tpName">Le nom du TP (ex: "TP1")</param>
    /// <param name="teamNumber">Le numéro de l'équipe</param>
    /// <returns>La grille générée</returns>
    GridModel GenerateTeamGrid(List<StudentModel> teamStudents, RubricModel rubric, string tpName, int teamNumber);

    /// <summary>
    /// Vérifie si une grille existe déjà pour une équipe
    /// </summary>
    /// <param name="teamStudents">La liste des étudiants de l'équipe</param>
    /// <param name="basePath">Le chemin de base de la session</param>
    /// <param name="teamNumber">Le numéro de l'équipe</param>
    /// <returns>True si la grille existe</returns>
    bool TeamGridExists(List<StudentModel> teamStudents, string basePath, int teamNumber);

    /// <summary>
    /// Sauvegarde une grille d'équipe dans le dossier grading approprié
    /// </summary>
    /// <param name="grid">La grille d'équipe à sauvegarder</param>
    /// <param name="teamStudents">La liste des étudiants de l'équipe</param>
    /// <param name="teamNumber">Le numéro de l'équipe</param>
    /// <param name="basePath">Le chemin de base de la session (ex: sessions/Hiver 2026/BD1/TP1)</param>
    /// <returns>True si sauvegardé avec succès</returns>
    Task<bool> SaveTeamGridAsync(GridModel grid, List<StudentModel> teamStudents, int teamNumber, string basePath);

    /// <summary>
    /// Charge tous les fichiers de grille JSON d'un dossier grading et retourne les informations des fichiers
    /// </summary>
    /// <param name="gradingPath">Le chemin du dossier grading contenant les fichiers JSON</param>
    /// <returns>Liste des informations des fichiers de grille triés</returns>
    List<GridFileInfo> LoadGridFiles(string gradingPath);

    /// <summary>
    /// Charge une grille complète depuis un fichier JSON
    /// </summary>
    /// <param name="filePath">Le chemin du fichier JSON de la grille</param>
    /// <returns>La grille chargée</returns>
    Task<GridModel?> LoadGridAsync(string filePath);

    /// <summary>
    /// Recommande un résultat pour un critère en combinant les sévérités des commentaires sélectionnés.
    /// </summary>
    /// <param name="feedback">Les commentaires actifs sur le critère (avec leur sévérité).</param>
    /// <param name="scale">L'échelle du critère.</param>
    /// <returns>Le qualificatif recommandé, ou null si aucun feedback.</returns>
    string? GetResultRecommendation(
        IEnumerable<CommentEntry> feedback,
        IEnumerable<ScaleItemModel> scale);

    /// <summary>
    /// Cherche toutes les grilles du dossier grading qui contiennent un commentaire identique (texte + sévérité).
    /// </summary>
    List<string> FindCommentUsages(string gradingPath, CommentEntry comment);
}