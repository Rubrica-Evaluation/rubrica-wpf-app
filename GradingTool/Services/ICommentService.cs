namespace GradingTool.Services;

public interface ICommentService
{
    /// <summary>
    /// Obtient les commentaires suggérés pour un critère donné
    /// </summary>
    /// <param name="criterionLabel">Le label du critère</param>
    /// <returns>Liste des commentaires suggérés</returns>
    List<string> GetCommentsForCriterion(string criterionLabel);

    /// <summary>
    /// Ajoute un commentaire à la banque pour un critère
    /// </summary>
    /// <param name="criterionLabel">Le label du critère</param>
    /// <param name="comment">Le commentaire à ajouter</param>
    void AddCommentForCriterion(string criterionLabel, string comment);

    /// <summary>
    /// Sauvegarde les commentaires dans un fichier JSON
    /// </summary>
    /// <param name="gradingPath">Le chemin du répertoire grading</param>
    Task SaveCommentsAsync(string gradingPath);

    /// <summary>
    /// Charge les commentaires depuis un fichier JSON
    /// </summary>
    /// <param name="gradingPath">Le chemin du répertoire grading</param>
    Task LoadCommentsAsync(string gradingPath);
}
