namespace GradingTool.Services;

using GradingTool.Models;

public interface ICommentService
{
    List<CommentEntry> GetCommentsForCriterion(string criterionLabel);
    void AddCommentForCriterion(string criterionLabel, CommentEntry entry);
    void UpdateCommentForCriterion(string criterionLabel, string oldText, CommentEntry newEntry);
    void RemoveCommentForCriterion(string criterionLabel, string commentText);
    Task SaveCommentsAsync(string gradingPath);
    Task LoadCommentsAsync(string gradingPath);
}
