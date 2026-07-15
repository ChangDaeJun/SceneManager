using System.Windows;
using SceneManager.ViewModels;

namespace SceneManager.Views;

/// <summary>
/// 스냅샷 3단계: 실행 순서·의존성 편집 창. 확인 시 순환 의존을 검증하고, 문제가 있으면 닫지 않는다.
/// </summary>
public partial class SnapshotPriorityWindow : Window
{
    private readonly SnapshotPriorityViewModel _vm;

    public SnapshotPriorityWindow(SnapshotPriorityViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        // 콤보/셀 편집 값이 커밋되도록 포커스를 확정한다.
        OrderGrid.CommitEdit();

        if (!_vm.TryFinish(out var error))
        {
            MessageBox.Show(this, error, "의존성 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }
}
