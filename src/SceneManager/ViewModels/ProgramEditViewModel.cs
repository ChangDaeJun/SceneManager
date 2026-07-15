using CommunityToolkit.Mvvm.ComponentModel;
using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;

namespace SceneManager.ViewModels;

/// <summary>
/// 미세조정 창의 프로그램 한 행. DataGrid 편집과 배치도의 원천이 된다.
/// X/Y/W/H나 상태를 바꾸면 <see cref="Entry"/>의 창 배치를 갱신하고, 살아있는 창이면 즉시 실제로 반영한다.
/// </summary>
public partial class ProgramEditViewModel : ObservableObject
{
    /// <summary>상태 컬럼(콤보박스)에 제공하는 선택지. 최소화는 미세조정 대상이 아니라 제외.</summary>
    public static IReadOnlyList<WindowState> StateOptions { get; } =
        new[] { WindowState.Normal, WindowState.Maximized };

    private readonly IDesktopManager _desktop;
    private bool _suppressApply;

    /// <summary>원본 프로그램 항목(편집이 이 항목의 Window에 반영된다).</summary>
    public ProgramEntry Entry { get; }

    /// <summary>대응하는 살아있는 창 핸들. 없으면 실제 반영은 하지 않는다.</summary>
    public IntPtr? Handle { get; }

    public string Name => Entry.Name;
    public string Type => Entry.Type.ToString();
    public string? ExecPath => Entry.ExecPath;

    /// <summary>살아있는 창에 연결됐는지(회색 처리 등 표시에 사용).</summary>
    public bool IsLive => Handle is not null;

    [ObservableProperty] private double x;
    [ObservableProperty] private double y;
    [ObservableProperty] private double width;
    [ObservableProperty] private double height;
    [ObservableProperty] private WindowState state;

    public ProgramEditViewModel(ProgramEntry entry, IntPtr? handle, IDesktopManager desktop)
    {
        Entry = entry;
        Handle = handle;
        _desktop = desktop;

        var w = entry.Window;
        x = w?.X ?? 0;
        y = w?.Y ?? 0;
        width = w?.Width ?? 0;
        height = w?.Height ?? 0;
        state = w?.State ?? WindowState.Normal;
    }

    partial void OnXChanged(double value) => OnCoordEdited();
    partial void OnYChanged(double value) => OnCoordEdited();
    partial void OnWidthChanged(double value) => OnCoordEdited();
    partial void OnHeightChanged(double value) => OnCoordEdited();
    partial void OnStateChanged(WindowState value) => PushToWindow();

    /// <summary>
    /// 좌표를 직접 지정했다는 건 "이 위치의 일반 창"으로 만들겠다는 의미다. 최대화 상태면 x/y/w/h가
    /// 무시되므로 Normal로 전환(콤보박스도 갱신)한 뒤 한 번만 실제로 반영한다.
    /// </summary>
    private void OnCoordEdited()
    {
        if (_suppressApply)
            return;

        if (State != WindowState.Normal)
        {
            _suppressApply = true;
            State = WindowState.Normal; // 콤보박스 반영(중복 반영은 억제)
            _suppressApply = false;
        }

        PushToWindow();
    }

    /// <summary>표시값(X/Y/W/H/State)을 창 배치에 쓰고, 살아있는 창이면 실제로 반영한다.</summary>
    private void PushToWindow()
    {
        if (_suppressApply || Entry.Window is null)
            return;

        Entry.Window.X = X;
        Entry.Window.Y = Y;
        Entry.Window.Width = Width;
        Entry.Window.Height = Height;
        Entry.Window.State = State;

        if (Handle is { } handle)
            _desktop.SetPlacement(handle, Entry.Window);
    }

    /// <summary>지정한 배치로 되돌린다(실행취소 등). 표시·창에 모두 반영한다.</summary>
    public void RestorePlacement(double x, double y, double width, double height, WindowState state)
    {
        if (Entry.Window is null)
            return;

        Entry.Window.X = x;
        Entry.Window.Y = y;
        Entry.Window.Width = width;
        Entry.Window.Height = height;
        Entry.Window.State = state;
        ReloadFromEntry();
    }

    /// <summary>
    /// 외부(예: 틈 메우기)가 <see cref="Entry"/>.Window를 직접 바꾼 뒤 호출한다.
    /// 표시값을 다시 읽고(속성 변경 알림 → 지도 갱신) 살아있는 창을 한 번만 반영한다.
    /// </summary>
    public void ReloadFromEntry()
    {
        if (Entry.Window is null)
            return;

        _suppressApply = true; // 속성별 반영 억제(중복 이동 방지)
        X = Entry.Window.X;
        Y = Entry.Window.Y;
        Width = Entry.Window.Width;
        Height = Entry.Window.Height;
        State = Entry.Window.State;
        _suppressApply = false;

        if (Handle is { } handle)
            _desktop.SetPlacement(handle, Entry.Window);
    }
}
