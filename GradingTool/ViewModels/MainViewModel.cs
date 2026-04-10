using CommunityToolkit.Mvvm.ComponentModel;
using GradingTool.Services;

namespace GradingTool.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    public INavigationService NavigationService => _navigationService;

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public void NavigateToInitialView()
    {
        _navigationService.NavigateTo<WorkspaceViewModel>();
    }
}
