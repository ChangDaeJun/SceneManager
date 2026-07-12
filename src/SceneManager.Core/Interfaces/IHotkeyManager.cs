namespace SceneManager.Core.Interfaces;

/// <summary>
/// 글로벌 단축키(핫키)의 등록·해제·충돌 검사를 담당한다.
/// </summary>
public interface IHotkeyManager
{
    /// <summary>
    /// 핫키를 등록하고, 눌렸을 때 실행할 콜백을 연결한다.
    /// </summary>
    /// <param name="hotkeyString">핫키 표현. 예: <c>Ctrl+Alt+1</c></param>
    /// <param name="callback">핫키가 눌렸을 때 실행할 동작.</param>
    HotkeyRegistrationResult Register(string hotkeyString, Action callback);

    /// <summary>지정한 핫키를 해제한다.</summary>
    void Unregister(string hotkeyString);

    /// <summary>등록된 모든 핫키를 해제한다.</summary>
    void UnregisterAll();

    /// <summary>해당 핫키가 이미(다른 곳에) 등록되어 충돌하는지 검사한다.</summary>
    bool IsConflicting(string hotkeyString);
}

/// <summary>
/// 핫키 등록 결과.
/// </summary>
public sealed class HotkeyRegistrationResult
{
    /// <summary>등록 성공 여부.</summary>
    public bool Success { get; set; }

    /// <summary>충돌이 원인이면, 충돌한 대상(핫키 또는 소유자). 없으면 null.</summary>
    public string? ConflictWith { get; set; }

    /// <summary>실패 시 오류 메시지. 성공 시 null.</summary>
    public string? ErrorMessage { get; set; }
}
