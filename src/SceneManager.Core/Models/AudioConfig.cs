namespace SceneManager.Core.Models;

/// <summary>
/// 특정 프로세스의 볼륨 지정. Windows 볼륨 믹서의 앱별 세션에 대응된다.
/// </summary>
public sealed class AppVolumeEntry
{
    /// <summary>대상 프로세스명. 예: <c>chrome.exe</c></summary>
    public required string ProcessName { get; set; }

    /// <summary>볼륨 레벨(0~100).</summary>
    public int Volume { get; set; }
}

/// <summary>
/// 씬에 저장되는 오디오 설정. 마스터/입력 볼륨, 기본 입출력 장치,
/// 앱별 볼륨을 담는다.
/// </summary>
public sealed class AudioConfig
{
    /// <summary>마스터 볼륨(0~100).</summary>
    public int MasterVolume { get; set; }

    /// <summary>기본 출력 장치. 장치명 또는 장치 ID. 미지정 시 변경하지 않음.</summary>
    public string? DefaultOutputDevice { get; set; }

    /// <summary>기본 입력 장치. 장치명 또는 장치 ID. 미지정 시 변경하지 않음.</summary>
    public string? DefaultInputDevice { get; set; }

    /// <summary>입력(마이크) 볼륨(0~100).</summary>
    public int InputVolume { get; set; }

    /// <summary>앱별 볼륨 목록.</summary>
    public List<AppVolumeEntry> AppVolumes { get; set; } = [];
}
