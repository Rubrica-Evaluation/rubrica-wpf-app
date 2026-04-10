namespace GradingTool.Services;

using GradingTool.Models;

public interface ICommentService
{
    /// <summary>
    /// Retourne une copie des commentaires réutilisables associés à un critère.
    /// </summary>
    List<CommentEntry> GetCommentsForCriterion(string criterionLabel);

    /// <summary>
    /// Ajoute un commentaire à la banque d'un critère, en ignorant les doublons (insensible à la casse).
    /// </summary>
    void AddCommentForCriterion(string criterionLabel, CommentEntry entry);

    /// <summary>
    /// Remplace un commentaire existant (identifié par <paramref name="oldText"/>) par <paramref name="newEntry"/>.
    /// </summary>
    void UpdateCommentForCriterion(string criterionLabel, string oldText, CommentEntry newEntry);

    /// <summary>
    /// Supprime le commentaire dont le texte correspond à <paramref name="commentText"/> (insensible à la casse).
    /// </summary>
    void RemoveCommentForCriterion(string criterionLabel, string commentText);

    /// <summary>
    /// Sérialise la banque de commentaires dans <c>reusable_comments.json</c> sous <paramref name="gradingPath"/>.
    /// N'écrit rien si le chemin ne correspond pas au dernier chargement réussi.
    /// </summary>
    Task SaveCommentsAsync(string gradingPath);

    /// <summary>
    /// Charge la banque de commentaires depuis <c>reusable_comments.json</c> sous <paramref name="gradingPath"/>.
    /// Si le fichier est absent, le cache est vidé. En cas d'erreur de lecture, le cache existant est conservé.
    /// </summary>
    Task LoadCommentsAsync(string gradingPath);
}
