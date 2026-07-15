using System.Windows;
using SceneManager.ViewModels;

namespace SceneManager.Views;

/// <summary>
/// 스냅샷 2단계: 프로그램별 실행 인자 확인 창. 인자를 모를 때 "인자 찾기 도우미"로
/// AI에게 물어볼 프롬프트를 띄워 준다.
/// </summary>
public partial class SnapshotArgumentsWindow : Window
{
    private readonly SnapshotArgumentsViewModel _vm;

    public SnapshotArgumentsWindow(SnapshotArgumentsViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;
    }

    private void OnShowHelper(object sender, RoutedEventArgs e)
    {
        var program = _vm.CurrentProgram;
        if (program is null)
            return;

        // 입력 중이던 인자를 먼저 반영(도우미 후 이어서 편집할 수 있도록).
        ArgsBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();

        if (_vm.BuildHelperPrompt() is not { } prompt)
            return;

        var dialog = new PromptDialog(
            $"인자 찾기 도우미 — {program.Name}",
            prompt.Korean,
            prompt.English)
        {
            Owner = this,
        };
        dialog.ShowDialog();
    }

    private void OnOk(object sender, RoutedEventArgs e) => DialogResult = true;
}
