namespace SceneManager.Core.Interfaces;

/// <summary>
/// 씬 적용의 오케스트레이터. 검증 → 이전 씬 정리 → 프로그램 실행 → 창 배치 → 오디오 적용의
/// 전체 흐름을 조율한다. 프로세스·윈도우·모니터·오디오 매니저를 조합해 동작한다.
/// </summary>
public interface ISceneEngine
{
    /// <summary>씬을 적용한다(전체 플로우).</summary>
    Task<SceneApplyResult> ApplyAsync(string sceneId, CancellationToken cancellationToken = default);

    /// <summary>현재 적용 중인 씬의 ID. 적용된 씬이 없으면 null.</summary>
    string? CurrentSceneId { get; }

    /// <summary>씬 적용 진행 상황이 갱신될 때 발생한다.</summary>
    event EventHandler<SceneProgressEventArgs> ProgressChanged;
}

/// <summary>
/// 씬 적용 결과. 개별 단계의 성공/실패를 모두 담는다(부분 실패 허용).
/// </summary>
public sealed class SceneApplyResult
{
    /// <summary>전체 적용 성공 여부.</summary>
    public bool Success { get; set; }

    /// <summary>적용한 씬의 ID.</summary>
    public required string SceneId { get; set; }

    /// <summary>적용에 걸린 시간.</summary>
    public TimeSpan Elapsed { get; set; }

    /// <summary>단계별 결과 목록.</summary>
    public List<StepResult> Steps { get; set; } = [];
}

/// <summary>
/// 씬 적용 중 한 단계의 결과.
/// </summary>
public sealed class StepResult
{
    /// <summary>단계 이름. 예: <c>Launch Chrome</c>, <c>Set Audio</c></summary>
    public required string StepName { get; set; }

    /// <summary>이 단계의 성공 여부.</summary>
    public bool Success { get; set; }

    /// <summary>실패 시 오류 메시지. 성공 시 null.</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 씬 적용 진행 상황 이벤트 데이터.
/// </summary>
public sealed class SceneProgressEventArgs : EventArgs
{
    /// <summary>현재 단계 설명.</summary>
    public required string StepDescription { get; set; }

    /// <summary>현재 단계 번호(1부터).</summary>
    public int CurrentStep { get; set; }

    /// <summary>전체 단계 수.</summary>
    public int TotalSteps { get; set; }

    /// <summary>진행률(0~100).</summary>
    public double ProgressPercent { get; set; }
}
