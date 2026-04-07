using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace GradingTool.Services;

public partial class NavigationService : ObservableObject, INavigationService
{
    private readonly Func<Type, object> _viewModelFactory;
    private object? _previousView;

    [ObservableProperty]
    private object? _currentView;

    public NavigationService(Func<Type, object> viewModelFactory)
    {
        _viewModelFactory = viewModelFactory;
    }

    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        // Sauvegarder la vue actuelle comme vue précédente
        _previousView = CurrentView;
        
        var viewModel = _viewModelFactory.Invoke(typeof(TViewModel));
        CurrentView = viewModel;
        (CurrentView as IActivatable)?.OnActivated();
    }

    public void NavigateBack()
    {
        // Restaurer la vue précédente
        if (_previousView != null)
        {
            CurrentView = _previousView;
            _previousView = null;
            (CurrentView as IActivatable)?.OnActivated();
        }
    }
}
