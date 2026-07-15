using System.Windows;
using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;
using SceneManager.Core.Services;
using SceneManager.ViewModels;
using SceneManager.Views;

namespace SceneManager.Services;

/// <summary>WPF 구현: <see cref="SnapshotNameDialog"/>와 <see cref="MessageBox"/> 사용.</summary>
public sealed class DialogService : IDialogService
{
    private readonly ArgumentAdvisor _advisor;

    public DialogService(ArgumentAdvisor advisor) => _advisor = advisor;

    public string? PromptSceneName(string? initial = null)
    {
        var dialog = new SnapshotNameDialog(initial) { Owner = Application.Current.MainWindow };
        return dialog.ShowDialog() == true ? dialog.SceneName : null;
    }

    public bool ShowSnapshotFineTune(Scene scene, MonitorLayout monitors, IDesktopManager desktop)
    {
        var viewModel = new SnapshotFineTuneViewModel(scene, monitors, desktop);
        var window = new SnapshotFineTuneWindow(viewModel) { Owner = Application.Current.MainWindow };
        return window.ShowDialog() == true;
    }

    public bool ShowSnapshotArguments(Scene scene, MonitorLayout monitors)
    {
        var viewModel = new SnapshotArgumentsViewModel(scene, monitors, _advisor);
        var window = new SnapshotArgumentsWindow(viewModel) { Owner = Application.Current.MainWindow };
        return window.ShowDialog() == true;
    }

    public bool Confirm(string message, string title)
        => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
           == MessageBoxResult.Yes;

    public void Info(string message, string title)
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
}
