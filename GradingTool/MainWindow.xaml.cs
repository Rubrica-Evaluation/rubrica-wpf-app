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
        if (DataContext is not MainViewModel mainViewModel)
            return;

        var currentView = mainViewModel.NavigationService.CurrentView;

        // Vérifier les modifications non enregistrées dans le Concepteur de grille
        if (currentView is RubricDesignerViewModel rubricDesigner)
        {
            if (!rubricDesigner.CanProceedWithUnsavedChanges())
            {
                e.Cancel = true;
                return;
            }
        }

        // Vérifier les modifications non enregistrées dans l'éditeur de groupes
        if (currentView is RosterEditorViewModel rosterEditor)
        {
            if (!rosterEditor.CanProceedWithUnsavedChanges())
            {
                e.Cancel = true;
                return;
            }
        }

        // Sauvegarder automatiquement la grille actuelle si on est dans l'éditeur
        if (currentView is GridEditorViewModel gridEditorViewModel)
        {
            await gridEditorViewModel.SaveCurrentGridAsync();
        }
    }
}