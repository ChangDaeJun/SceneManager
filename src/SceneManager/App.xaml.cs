using System.Windows;
using SceneManager.Core.Persistence;
using SceneManager.Core.Platform;
using SceneManager.Core.Services;
using SceneManager.Services;
using SceneManager.ViewModels;
using SceneManager.Views;

namespace SceneManager;

/// <summary>
/// 합성 루트. Core 서비스를 수동으로 조립해 MainViewModel을 구성하고 창을 띄운다.
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Core 조립 (러너와 동일한 경로 규칙)
        var repository = new JsonSceneRepository(AppPaths.ScenesDir);
        var filter = new JsonProcessFilterRepository(AppPaths.ProcessFilterFile).LoadOrCreateDefault();
        var desktop = new WindowsDesktopManager();
        var snapshot = new SceneSnapshot(desktop, filter);

        var viewModel = new MainViewModel(repository, snapshot, desktop, new DialogService(), RunnerLocator.Find());

        var window = new MainWindow { DataContext = viewModel };
        MainWindow = window;
        window.Show();

        await viewModel.LoadAsync();
    }
}
