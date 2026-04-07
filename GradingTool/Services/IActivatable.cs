namespace GradingTool.Services;

/// <summary>
/// Permet à un ViewModel d'être notifié quand il redevient la vue active (NavigateBack ou NavigateTo).
/// </summary>
public interface IActivatable
{
    void OnActivated();
}
