using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;

namespace SceneManager.Services;

/// <summary>ViewModel이 UI(대화상자)와 상호작용하기 위한 최소 추상화.</summary>
public interface IDialogService
{
    /// <summary>씬 이름을 입력받는다. 취소 시 null.</summary>
    string? PromptSceneName(string? initial = null);

    /// <summary>예/아니오 확인.</summary>
    bool Confirm(string message, string title);

    /// <summary>정보 알림.</summary>
    void Info(string message, string title);

    /// <summary>
    /// 스냅샷 미세조정 창을 모달로 띄운다. 확인 시 true(저장), 취소 시 false.
    /// 편집 중 <paramref name="scene"/>의 창 배치가 갱신되고 실제 창이 이동한다.
    /// </summary>
    bool ShowSnapshotFineTune(Scene scene, MonitorLayout monitors, IDesktopManager desktop);

    /// <summary>
    /// 스냅샷 인자 확인 창(2단계)을 모달로 띄운다. 확인 시 true(저장), 취소 시 false.
    /// 프로그램별 <see cref="ProgramEntry.Arguments"/>가 <paramref name="scene"/>에 반영된다.
    /// </summary>
    bool ShowSnapshotArguments(Scene scene, MonitorLayout monitors);

    /// <summary>
    /// 스냅샷 우선순위·의존성 창(3단계)을 모달로 띄운다. 확인 시 true(저장), 취소 시 false.
    /// <see cref="ProgramEntry.Order"/>·<see cref="ProgramEntry.DependsOnId"/>가 <paramref name="scene"/>에 반영된다.
    /// </summary>
    bool ShowSnapshotPriority(Scene scene);
}
