using CommunityToolkit.Mvvm.ComponentModel;
using SceneManager.Core.Models;

namespace SceneManager.ViewModels;

/// <summary>의존 대상 선택지. Id가 null이면 "(없음)".</summary>
public sealed record DependencyOption(string? Id, string Label);

/// <summary>
/// 우선순위/의존성 창의 프로그램 한 행. 표시 순번과 의존 대상(<see cref="ProgramEntry.DependsOnId"/>),
/// 실행 후 지연(<see cref="ProgramEntry.DelayAfterMs"/>)을 편집한다.
/// </summary>
public partial class ProgramOrderViewModel : ObservableObject
{
    public ProgramEntry Entry { get; }

    public string Name => Entry.Name;
    public string Type => Entry.Type.ToString();

    /// <summary>이 행이 의존할 수 있는 대상들(자기 자신 제외 + "(없음)").</summary>
    public IReadOnlyList<DependencyOption> DependencyOptions { get; }

    /// <summary>표시용 순번(1부터). 목록 위치가 바뀌면 부모가 갱신한다.</summary>
    [ObservableProperty] private int position;

    /// <summary>선택된 의존 대상. 바뀌면 <see cref="ProgramEntry.DependsOnId"/>에 반영한다.</summary>
    [ObservableProperty] private DependencyOption? selectedDependency;

    public ProgramOrderViewModel(ProgramEntry entry, IReadOnlyList<DependencyOption> dependencyOptions)
    {
        Entry = entry;
        DependencyOptions = dependencyOptions;
        selectedDependency = dependencyOptions.FirstOrDefault(o => o.Id == entry.DependsOnId)
                             ?? dependencyOptions[0]; // [0] == "(없음)"
    }

    /// <summary>실행 후 다음 프로그램까지 대기(ms). 표에서 직접 편집.</summary>
    public int DelayAfterMs
    {
        get => Entry.DelayAfterMs;
        set
        {
            if (Entry.DelayAfterMs == value)
                return;
            Entry.DelayAfterMs = value < 0 ? 0 : value;
            OnPropertyChanged();
        }
    }

    partial void OnSelectedDependencyChanged(DependencyOption? value)
        => Entry.DependsOnId = value?.Id;
}
