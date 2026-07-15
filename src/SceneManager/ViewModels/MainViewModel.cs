using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;
using SceneManager.Core.Services;
using SceneManager.Services;

namespace SceneManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISceneRepository _repository;
    private readonly ISceneSnapshot _snapshot;
    private readonly IDesktopManager _desktop;
    private readonly IDialogService _dialogs;
    private readonly string? _runnerPath;

    public ObservableCollection<Scene> Scenes { get; } = new();

    [ObservableProperty]
    private Scene? selectedScene;

    [ObservableProperty]
    private MonitorLayout monitors = new();

    public MainViewModel(
        ISceneRepository repository,
        ISceneSnapshot snapshot,
        IDesktopManager desktop,
        IDialogService dialogs,
        string? runnerPath)
    {
        _repository = repository;
        _snapshot = snapshot;
        _desktop = desktop;
        _dialogs = dialogs;
        _runnerPath = runnerPath;
        Monitors = _desktop.GetMonitorLayout();
    }

    /// <summary>씬 목록을 다시 로드한다.</summary>
    public async Task LoadAsync()
    {
        var all = await _repository.GetAllAsync();
        Scenes.Clear();
        foreach (var s in all.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
            Scenes.Add(s);
    }

    [RelayCommand]
    private async Task SnapshotAsync()
    {
        var name = _dialogs.PromptSceneName();
        if (string.IsNullOrWhiteSpace(name))
            return;

        var existing = await _repository.GetByNameAsync(name);
        if (existing is not null &&
            !_dialogs.Confirm($"'{name}' 씬이 이미 있습니다. 덮어쓸까요?", "덮어쓰기"))
            return;

        var scene = await _snapshot.CaptureFullAsync(name);

        Monitors = _desktop.GetMonitorLayout(); // 캡처 시점 구성 반영(미세조정 지도용)

        // 미세조정 창: 방금 캡처한 창들이 살아있으므로 좌표 조정이 실제 창에 즉시 반영된다.
        // 확인 시에만 저장하고, 취소하면 저장하지 않는다.
        if (!_dialogs.ShowSnapshotFineTune(scene, Monitors, _desktop))
            return;

        await _repository.SaveAsync(scene);
        await LoadAsync();
        SelectedScene = Scenes.FirstOrDefault(
            s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteAsync()
    {
        var scene = SelectedScene!;
        if (!_dialogs.Confirm($"'{scene.Name}' 씬을 삭제할까요?", "삭제"))
            return;

        await _repository.DeleteAsync(scene.Id);
        await LoadAsync();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void CreateShortcut()
    {
        if (_runnerPath is null)
        {
            _dialogs.Info("SceneRunner.exe를 찾을 수 없습니다. 실행기를 먼저 빌드하세요.", "바로가기");
            return;
        }

        var path = ShortcutService.CreateOnDesktop(SelectedScene!, _runnerPath);
        _dialogs.Info($"바탕화면에 바로가기를 만들었습니다:\n{path}", "바로가기");
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Edit()
        => _dialogs.Info("씬 편집 화면은 2차에서 구현됩니다.", "편집");

    /// <summary>선택 씬의 창들 사이 작은 틈·겹침을 모서리 정렬로 메운다.</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task TidyLayoutAsync()
    {
        var scene = SelectedScene!;
        var changed = LayoutTidy.SnapGaps(scene, Monitors);
        if (changed == 0)
        {
            _dialogs.Info("메울 만한 틈이 없습니다.", "틈 메우기");
            return;
        }

        await _repository.SaveAsync(scene);
        await LoadAsync();
        SelectedScene = Scenes.FirstOrDefault(s => s.Id == scene.Id);
        _dialogs.Info($"{changed}개 창의 위치를 정렬했습니다.", "틈 메우기");
    }

    private bool HasSelection => SelectedScene is not null;

    partial void OnSelectedSceneChanged(Scene? value)
    {
        DeleteCommand.NotifyCanExecuteChanged();
        CreateShortcutCommand.NotifyCanExecuteChanged();
        EditCommand.NotifyCanExecuteChanged();
        TidyLayoutCommand.NotifyCanExecuteChanged();
    }
}
