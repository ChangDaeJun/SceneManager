using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SceneManager.Core.Models;
using SceneManager.Core.Services;

namespace SceneManager.ViewModels;

/// <summary>
/// 스냅샷 위저드 3단계: 실행 우선순위(순서)와 의존성 편집. 순서는 위/아래 이동으로 바꾸고
/// (<see cref="ProgramEntry.Order"/>에 반영), 의존 대상은 행마다 콤보로 고른다.
/// 확인 시 <see cref="DependencyResolver"/>로 순환 의존을 검증한다.
/// </summary>
public partial class SnapshotPriorityViewModel : ObservableObject
{
    public Scene Scene { get; }
    public ObservableCollection<ProgramOrderViewModel> Programs { get; } = new();

    [ObservableProperty] private ProgramOrderViewModel? selectedRow;

    public SnapshotPriorityViewModel(Scene scene)
    {
        Scene = scene;

        var ordered = scene.Programs
            .Select((p, i) => (p, i))
            .OrderBy(t => t.p.Order).ThenBy(t => t.i)
            .Select(t => t.p)
            .ToList();

        foreach (var p in ordered)
            Programs.Add(new ProgramOrderViewModel(p, BuildOptions(scene.Programs, p)));

        Reindex(); // Order를 0..n-1로 정규화하고 표시 순번을 매긴다
    }

    private static IReadOnlyList<DependencyOption> BuildOptions(IEnumerable<ProgramEntry> all, ProgramEntry self)
    {
        var options = new List<DependencyOption> { new(null, "(없음)") };
        options.AddRange(all.Where(p => !ReferenceEquals(p, self))
                            .Select(p => new DependencyOption(p.Id, p.Name)));
        return options;
    }

    partial void OnSelectedRowChanged(ProgramOrderViewModel? value)
    {
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp()
    {
        var i = SelectedRow is null ? -1 : Programs.IndexOf(SelectedRow);
        if (i <= 0)
            return;
        Programs.Move(i, i - 1);
        Reindex();
        NotifyMoveState();
    }

    private bool CanMoveUp() => SelectedRow is not null && Programs.IndexOf(SelectedRow) > 0;

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown()
    {
        var i = SelectedRow is null ? -1 : Programs.IndexOf(SelectedRow);
        if (i < 0 || i >= Programs.Count - 1)
            return;
        Programs.Move(i, i + 1);
        Reindex();
        NotifyMoveState();
    }

    private bool CanMoveDown() => SelectedRow is not null && Programs.IndexOf(SelectedRow) < Programs.Count - 1;

    /// <summary>확인 시 호출. 순환 의존이 없으면 true, 있으면 false + 메시지.</summary>
    public bool TryFinish(out string? error)
    {
        try
        {
            DependencyResolver.Resolve(Scene.Programs);
            error = null;
            return true;
        }
        catch (CircularDependencyException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>목록 위치를 각 프로그램의 Order와 표시 순번에 반영한다.</summary>
    private void Reindex()
    {
        for (var i = 0; i < Programs.Count; i++)
        {
            Programs[i].Entry.Order = i;
            Programs[i].Position = i + 1;
        }
    }

    private void NotifyMoveState()
    {
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }
}
