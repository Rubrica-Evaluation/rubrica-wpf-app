using System.Configuration;
using System.Data;
using System.Text;
using System.Windows;
using GradingTool.Helpers;
using GradingTool.Services;
using GradingTool.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GradingTool;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Register encoding provider for Windows codepages
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        var sessionsRootService = _serviceProvider.GetRequiredService<ISessionsRootService>();
        var rootPath = sessionsRootService.GetSessionsRootPath();

        if (OneDriveHelper.ShouldWarnUser(rootPath))
        {
            var result = MessageBox.Show(
                "Le dossier de données se trouve dans OneDrive, mais OneDrive n'est pas démarré.\n\n"
                + "Cela pourrait entraîner une perte de données ou des conflits de synchronisation.\n\n"
                + "Conseil : fermez l'application, démarrez OneDrive, puis relancez.\n\n"
                + "Voulez-vous continuer malgré le risque ?",
                "Avertissement — OneDrive inactif",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result == MessageBoxResult.No)
            {
                Shutdown();
                return;
            }
        }
    }

    private void ConfigureServices(ServiceCollection services)
    {
        // Services
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<ISessionsRootService, SessionsRootService>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<ICourseService, CourseService>();
        services.AddSingleton<IWorkService, WorkService>();
        services.AddSingleton<IRubricService, RubricService>();
        services.AddSingleton<IRosterService, RosterService>();
        services.AddSingleton<IGridService, GridService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IPdfService, PdfService>();
        services.AddSingleton<ICommentService, CommentService>();

        // ViewModel Factory pour le NavigationService
        services.AddSingleton<Func<Type, object>>(serviceProvider =>
        {
            object ViewModelFactory(Type viewModelType)
            {
                return serviceProvider.GetRequiredService(viewModelType);
            }
            return ViewModelFactory;
        });

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<WorkspaceViewModel>();
        services.AddSingleton<GridEditorViewModel>();
        services.AddSingleton<RubricDesignerViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

