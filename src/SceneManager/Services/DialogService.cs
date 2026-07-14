using System.Windows;
using SceneManager.Views;

namespace SceneManager.Services;

/// <summary>WPF 구현: <see cref="SnapshotNameDialog"/>와 <see cref="MessageBox"/> 사용.</summary>
public sealed class DialogService : IDialogService
{
    public string? PromptSceneName(string? initial = null)
    {
        var dialog = new SnapshotNameDialog(initial) { Owner = Application.Current.MainWindow };
        return dialog.ShowDialog() == true ? dialog.SceneName : null;
    }

    public bool Confirm(string message, string title)
        => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
           == MessageBoxResult.Yes;

    public void Info(string message, string title)
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
}
