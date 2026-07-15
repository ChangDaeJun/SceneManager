using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SceneManager.Core.Models;
using SceneManager.Core.Services;

namespace SceneManager.ViewModels;

/// <summary>
/// 스냅샷 인자 단계(2단계) 뷰모델. 프로그램을 하나씩 넘겨보며(스테퍼) 지도에서 강조하고,
/// 각 프로그램의 실행 인자(<see cref="ProgramEntry.Arguments"/>)를 확인·입력하게 한다.
/// 인자를 모를 때는 "인자 찾기 도우미" 프롬프트를 만들어 준다(<see cref="ArgumentAdvisor"/>).
/// </summary>
public partial class SnapshotArgumentsViewModel : ObservableObject
{
    private readonly List<ProgramEntry> _programs;
    private readonly ArgumentAdvisor _advisor;

    public Scene Scene { get; }
    public MonitorLayout Monitors { get; }

    /// <summary>현재 단계의 프로그램. 지도 강조(양방향)·상세 표시의 기준.</summary>
    [ObservableProperty] private ProgramEntry? currentProgram;

    public SnapshotArgumentsViewModel(Scene scene, MonitorLayout monitors, ArgumentAdvisor advisor)
    {
        Scene = scene;
        Monitors = monitors;
        _advisor = advisor;
        _programs = scene.Programs.OrderBy(p => p.Order).ToList();
        currentProgram = _programs.FirstOrDefault();
    }

    public int Total => _programs.Count;
    public int CurrentIndex => CurrentProgram is null ? -1 : _programs.IndexOf(CurrentProgram);

    /// <summary>"2 / 5" 형태의 진행 표시.</summary>
    public string Position => Total == 0 ? "0 / 0" : $"{CurrentIndex + 1} / {Total}";

    /// <summary>인자 입력 칸 위 안내(상황별).</summary>
    public string Hint => CurrentProgram is null ? string.Empty : _advisor.Hint(CurrentProgram);

    /// <summary>현재 프로그램의 "인자 찾기 도우미" 프롬프트(한국어, 영어). 프로그램이 없으면 null.</summary>
    public (string Korean, string English)? BuildHelperPrompt()
        => CurrentProgram is null
            ? null
            : (_advisor.BuildKorean(CurrentProgram), _advisor.BuildEnglish(CurrentProgram));

    partial void OnCurrentProgramChanged(ProgramEntry? value)
    {
        OnPropertyChanged(nameof(Position));
        OnPropertyChanged(nameof(Hint));
        PrevCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanPrev))]
    private void Prev()
    {
        if (CanPrev())
            CurrentProgram = _programs[CurrentIndex - 1];
    }

    private bool CanPrev() => CurrentIndex > 0;

    [RelayCommand(CanExecute = nameof(CanNext))]
    private void Next()
    {
        if (CanNext())
            CurrentProgram = _programs[CurrentIndex + 1];
    }

    private bool CanNext() => CurrentIndex >= 0 && CurrentIndex < Total - 1;
}
