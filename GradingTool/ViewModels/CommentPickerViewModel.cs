using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GradingTool.Models;
using GradingTool.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GradingTool.ViewModels;

public partial class CommentPickerViewModel : ObservableObject
{
    private readonly ICommentService _commentService;
    private readonly string _criterionLabel;
    private readonly ObservableCollection<CommentEntry> _allComments;

    public string CriterionLabel { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredComments))]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private CommentEntry? _selectedEntry;

    public bool HasSelection => SelectedEntry != null;

    public IEnumerable<CommentEntry> FilteredComments =>
        string.IsNullOrWhiteSpace(SearchText)
            ? _allComments
            : _allComments.Where(c => c.Text.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase));

    public event Action<bool>? CloseRequested;
    public event Action<string, CommentSeverity>? EditRequested;

    public CommentPickerViewModel(string criterionLabel, List<CommentEntry> comments, ICommentService commentService)
    {
        CriterionLabel = criterionLabel;
        _criterionLabel = criterionLabel;
        _commentService = commentService;
        _allComments = new ObservableCollection<CommentEntry>(comments);
    }

    [RelayCommand(CanExecute = nameof(HasEntrySelected))]
    private void Confirm() => CloseRequested?.Invoke(true);

    private bool HasEntrySelected() => SelectedEntry != null;

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    [RelayCommand(CanExecute = nameof(HasEntrySelected))]
    private void Edit() => EditRequested?.Invoke(SelectedEntry!.Text, SelectedEntry!.Severity);

    public void ApplyEdit(string updatedText, CommentSeverity severity)
    {
        if (string.IsNullOrWhiteSpace(updatedText) || SelectedEntry == null) return;
        var oldText = SelectedEntry.Text;
        var newEntry = new CommentEntry { Text = updatedText.Trim(), Severity = severity };
        _commentService.UpdateCommentForCriterion(_criterionLabel, oldText, newEntry);
        int idx = _allComments.IndexOf(SelectedEntry);
        if (idx >= 0) _allComments[idx] = newEntry;
        SelectedEntry = newEntry;
        OnPropertyChanged(nameof(FilteredComments));
    }

    [RelayCommand(CanExecute = nameof(HasEntrySelected))]
    private void Delete()
    {
        if (SelectedEntry == null) return;
        _commentService.RemoveCommentForCriterion(_criterionLabel, SelectedEntry.Text);
        _allComments.Remove(SelectedEntry);
        SelectedEntry = null;
        OnPropertyChanged(nameof(FilteredComments));
    }
}
