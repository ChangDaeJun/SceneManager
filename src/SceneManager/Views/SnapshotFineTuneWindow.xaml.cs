using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SceneManager.ViewModels;

namespace SceneManager.Views;

public partial class SnapshotFineTuneWindow : Window
{
    private readonly SnapshotFineTuneViewModel _vm;

    public SnapshotFineTuneWindow(SnapshotFineTuneViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;

        // 배치가 바뀌면(이동·크기·삭제·틈 메우기·실행취소) 지도를 다시 그린다.
        viewModel.LayoutChanged += (_, _) => Map.Refresh();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // 텍스트/숫자 입력 중에는 키를 가로채지 않는다(캐럿 이동·타이핑 유지).
        if (Keyboard.FocusedElement is TextBox)
            return;

        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;

        if (ctrl && e.Key == Key.Z)
        {
            if (_vm.UndoCommand.CanExecute(null))
            {
                _vm.UndoCommand.Execute(null);
                e.Handled = true;
            }
            return;
        }

        if (e.Key == Key.Delete)
        {
            if (_vm.DeleteSelectedCommand.CanExecute(null))
            {
                _vm.DeleteSelectedCommand.Execute(null);
                e.Handled = true;
            }
            return;
        }

        var (dx, dy) = e.Key switch
        {
            Key.Left => (-1d, 0d),
            Key.Right => (1d, 0d),
            Key.Up => (0d, -1d),
            Key.Down => (0d, 1d),
            _ => (0d, 0d),
        };
        if ((dx == 0 && dy == 0) || _vm.SelectedRow is null)
            return;

        if (ctrl)
            _vm.Resize(dx, dy); // Ctrl+방향키: 좌/위 축소, 우/아래 확대
        else
            _vm.Nudge(dx, dy);  // 방향키: 이동
        e.Handled = true;
    }

    private void OnOk(object sender, RoutedEventArgs e) => DialogResult = true;
}
