using System.Windows;

namespace SceneManager.Views;

public partial class SnapshotNameDialog : Window
{
    public string SceneName => NameBox.Text.Trim();

    public SnapshotNameDialog(string? initial)
    {
        InitializeComponent();
        NameBox.Text = initial ?? string.Empty;
        Loaded += (_, _) => { NameBox.SelectAll(); NameBox.Focus(); };
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SceneName))
            return; // 빈 이름은 저장 불가
        DialogResult = true;
    }
}
