using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;
using SceneManager.Core.Services;

namespace SceneManager.ViewModels;

/// <summary>
/// 스냅샷 직후 미세조정 창의 뷰모델. 캡처한 씬의 각 프로그램을 살아있는 창에 매칭해,
/// x/y/w/h·상태 편집이 실제 창에 즉시 반영되게 한다. 방향키 이동·크기, 삭제, 실행취소, 틈 메우기 제공.
/// </summary>
public partial class SnapshotFineTuneViewModel : ObservableObject
{
    private sealed record RowState(ProgramEditViewModel Row, double X, double Y, double W, double H, WindowState State);

    private readonly IDesktopManager _desktop;
    private readonly Stack<List<RowState>> _undo = new();
    private List<RowState> _baseline = new();
    private bool _capturing = true; // false일 때(틈 메우기·실행취소 중)는 변경을 실행취소로 잡지 않음

    public Scene Scene { get; }
    public MonitorLayout Monitors { get; }
    public ObservableCollection<ProgramEditViewModel> Programs { get; } = new();

    /// <summary>표(DataGrid)에서 선택된 행.</summary>
    [ObservableProperty] private ProgramEditViewModel? selectedRow;

    /// <summary>배치도에서 선택된 프로그램(지도 ↔ 표 동기화용).</summary>
    [ObservableProperty] private ProgramEntry? selectedEntry;

    /// <summary>창 모서리를 각지게(둥근 모서리 제거) 처리할지. 씬에 저장된다.</summary>
    [ObservableProperty] private bool squareCorners;

    /// <summary>배치가 바뀌어 지도 재그리기가 필요할 때 발생.</summary>
    public event EventHandler? LayoutChanged;

    public SnapshotFineTuneViewModel(Scene scene, MonitorLayout monitors, IDesktopManager desktop)
    {
        Scene = scene;
        Monitors = monitors;
        _desktop = desktop;
        squareCorners = scene.SquareCorners;

        var windows = desktop.GetAllVisibleWindows();
        var handles = WindowMatcher.ResolveHandles(scene.Programs, windows);

        foreach (var p in scene.Programs.OrderBy(p => p.Order))
        {
            IntPtr? handle = handles.TryGetValue(p.Id, out var h) ? h : null;
            var row = new ProgramEditViewModel(p, handle, desktop);
            row.PropertyChanged += OnRowChanged;
            Programs.Add(row);
        }

        _baseline = Snapshot();
    }

    // ────── 선택 동기화 ──────

    partial void OnSelectedRowChanged(ProgramEditViewModel? value)
    {
        SelectedEntry = value?.Entry;
        DeleteSelectedCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedEntryChanged(ProgramEntry? value)
        => SelectedRow = Programs.FirstOrDefault(r => ReferenceEquals(r.Entry, value));

    // ────── 각진 모서리(씬 옵션) ──────

    partial void OnSquareCornersChanged(bool value)
    {
        Scene.SquareCorners = value;
        foreach (var row in Programs)
        {
            if (row.Handle is { } handle)
                _desktop.SetCornerPreference(handle, value);
        }
    }

    // ────── 이동 / 크기 (방향키) ──────

    /// <summary>선택한 창을 (dx, dy)만큼 이동한다.</summary>
    public void Nudge(double dx, double dy)
    {
        if (SelectedRow is null)
            return;
        if (dx != 0) SelectedRow.X += dx;
        if (dy != 0) SelectedRow.Y += dy;
    }

    /// <summary>선택한 창의 크기를 (dw, dh)만큼 조절한다(최소 1).</summary>
    public void Resize(double dw, double dh)
    {
        if (SelectedRow is null)
            return;
        if (dw != 0) SelectedRow.Width = Math.Max(1, SelectedRow.Width + dw);
        if (dh != 0) SelectedRow.Height = Math.Max(1, SelectedRow.Height + dh);
    }

    // ────── 삭제 / 틈 메우기 / 실행취소 ──────

    /// <summary>선택한 프로그램을 씬에서 제거한다(창은 닫지 않음).</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelected()
    {
        var row = SelectedRow;
        if (row is null)
            return;

        _undo.Push(_baseline); // 삭제 전 상태(그 행 포함)
        row.PropertyChanged -= OnRowChanged;
        Programs.Remove(row);
        Scene.Programs.Remove(row.Entry);
        SelectedRow = null;
        SelectedEntry = null;

        _baseline = Snapshot();
        UndoCommand.NotifyCanExecuteChanged();
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>인접 창의 작은 틈·겹침을 정렬한다(한 번의 실행취소 단위).</summary>
    [RelayCommand]
    private void TidyLayout()
    {
        _undo.Push(_baseline);
        _capturing = false;
        LayoutTidy.SnapGaps(Scene, Monitors);
        foreach (var row in Programs)
            row.ReloadFromEntry();
        _capturing = true;

        _baseline = Snapshot();
        UndoCommand.NotifyCanExecuteChanged();
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>마지막 변경을 되돌린다(이동·크기·삭제·틈 메우기).</summary>
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (_undo.Count == 0)
            return;

        var snap = _undo.Pop();
        _capturing = false;

        // 삭제됐던 행을 다시 구독하기 위해 전체를 재구성한다.
        foreach (var row in Programs)
            row.PropertyChanged -= OnRowChanged;

        Programs.Clear();
        Scene.Programs.Clear();
        foreach (var rs in snap)
        {
            rs.Row.PropertyChanged += OnRowChanged;
            Programs.Add(rs.Row);
            Scene.Programs.Add(rs.Row.Entry);
            rs.Row.RestorePlacement(rs.X, rs.Y, rs.W, rs.H, rs.State);
        }

        SelectedRow = null;
        SelectedEntry = null;
        _baseline = Snapshot();
        _capturing = true;

        UndoCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    // ────── 실행취소 기록 ──────

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_capturing)
            return;

        if (e.PropertyName is nameof(ProgramEditViewModel.X)
            or nameof(ProgramEditViewModel.Y)
            or nameof(ProgramEditViewModel.Width)
            or nameof(ProgramEditViewModel.Height)
            or nameof(ProgramEditViewModel.State))
        {
            _undo.Push(_baseline);   // 이 변경 직전 상태
            _baseline = Snapshot();
            UndoCommand.NotifyCanExecuteChanged();
            LayoutChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private List<RowState> Snapshot()
        => Programs.Select(r => new RowState(r, r.X, r.Y, r.Width, r.Height, r.State)).ToList();

    private bool HasSelection => SelectedRow is not null;
    private bool CanUndo() => _undo.Count > 0;
}
