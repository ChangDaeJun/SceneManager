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
}
