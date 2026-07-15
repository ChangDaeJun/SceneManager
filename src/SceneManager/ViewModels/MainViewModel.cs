using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;
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

        // 1단계 미세조정: 방금 캡처한 창들이 살아있으므로 좌표 조정이 실제 창에 즉시 반영된다.
        // 확인 시에만 다음 단계로 넘어가고, 취소하면 저장하지 않는다.
        if (!_dialogs.ShowSnapshotFineTune(scene, Monitors, _desktop))
            return;

        // 2단계 실행 인자: 파일/URL이 열린 상태로 복원되도록 프로그램별 arguments를 확인한다.
        if (!_dialogs.ShowSnapshotArguments(scene, Monitors))
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

    /// <summary>
    /// 기존 씬을 스냅샷 생성과 동일한 위저드(이름 → 미세조정 → 인자)로 다시 편집한다.
    /// 기존 이름·인자를 기본값으로 유지하고, 확인 시에만 저장한다.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task EditAsync()
    {
        // 편집용 복사본을 디스크에서 새로 읽는다(취소해도 목록의 원본이 오염되지 않도록).
        var scene = await _repository.GetByNameAsync(SelectedScene!.Name);
        if (scene is null)
            return;

        var oldName = scene.Name;

        var name = _dialogs.PromptSceneName(oldName);
        if (string.IsNullOrWhiteSpace(name))
            return;

        var renamed = !string.Equals(name, oldName, StringComparison.OrdinalIgnoreCase);
        if (renamed)
        {
            var existing = await _repository.GetByNameAsync(name);
            if (existing is not null &&
                !_dialogs.Confirm($"'{name}' 씬이 이미 있습니다. 덮어쓸까요?", "덮어쓰기"))
                return;
        }
        scene.Name = name;

        Monitors = _desktop.GetMonitorLayout();

        // 스냅샷과 동일한 두 단계. 방금 캡처가 아니라 저장된 씬이라, 해당 창이 떠 있으면 실시간 반영된다.
        if (!_dialogs.ShowSnapshotFineTune(scene, Monitors, _desktop))
            return;
        if (!_dialogs.ShowSnapshotArguments(scene, Monitors))
            return;

        // 이름을 바꿨으면 이전 이름 파일을 먼저 지운다(파일명이 씬 이름 기준이라 중복 방지). 디스크엔 아직 이전 이름만 존재.
        if (renamed)
            await _repository.DeleteAsync(scene.Id);
        await _repository.SaveAsync(scene);

        await LoadAsync();
        SelectedScene = Scenes.FirstOrDefault(s => s.Id == scene.Id);
    }

    private bool HasSelection => SelectedScene is not null;

    partial void OnSelectedSceneChanged(Scene? value)
    {
        DeleteCommand.NotifyCanExecuteChanged();
        CreateShortcutCommand.NotifyCanExecuteChanged();
        EditCommand.NotifyCanExecuteChanged();
    }
}
