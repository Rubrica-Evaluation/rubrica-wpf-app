using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GradingTool.ViewModels;

namespace GradingTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Gérer la fermeture de l'application pour sauvegarder automatiquement
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Sauvegarder automatiquement la grille actuelle si on est dans l'éditeur
        if (DataContext is MainViewModel mainViewModel && 
            mainViewModel.NavigationService.CurrentView is GridEditorViewModel gridEditorViewModel)
        {
            await gridEditorViewModel.SaveCurrentGridAsync();
        }
    }
}