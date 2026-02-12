using GradingTool.ViewModels;

namespace GradingTool.Services;

public interface INavigationService
{
    object? CurrentView { get; }
    void NavigateTo<TViewModel>() where TViewModel : class;
    void NavigateBack();
}
