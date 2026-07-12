using SceneManager.Core.Models;

namespace SceneManager.Core.Interfaces;

/// <summary>
/// 오디오 상태를 조회하고 적용한다. 마스터/입력 볼륨, 기본 입출력 장치, 앱별 볼륨을 다룬다.
/// </summary>
public interface IAudioManager
{
    /// <summary>현재 오디오 상태를 스냅샷으로 읽는다.</summary>
    AudioConfig GetCurrentConfig();

    /// <summary>오디오 설정을 적용한다. 앱별 볼륨은 해당 프로세스 실행 후 반영된다.</summary>
    Task ApplyConfigAsync(AudioConfig config, CancellationToken cancellationToken = default);

    /// <summary>마스터 볼륨을 읽는다(0~100).</summary>
    int GetMasterVolume();

    /// <summary>마스터 볼륨을 설정한다(0~100).</summary>
    void SetMasterVolume(int volume);

    /// <summary>사용 가능한 출력 장치 목록.</summary>
    List<AudioDeviceInfo> GetOutputDevices();

    /// <summary>사용 가능한 입력 장치 목록.</summary>
    List<AudioDeviceInfo> GetInputDevices();

    /// <summary>기본 출력 장치를 전환한다.</summary>
    void SetDefaultOutputDevice(string deviceId);

    /// <summary>기본 입력 장치를 전환한다.</summary>
    void SetDefaultInputDevice(string deviceId);

    /// <summary>특정 앱의 볼륨을 설정한다(0~100).</summary>
    void SetAppVolume(string processName, int volume);

    /// <summary>특정 앱의 볼륨을 읽는다. 오디오 세션이 없으면 null.</summary>
    int? GetAppVolume(string processName);
}

/// <summary>
/// 오디오 장치의 방향(용도).
/// </summary>
public enum AudioDeviceType
{
    /// <summary>출력(스피커, 헤드셋 등).</summary>
    Output,

    /// <summary>입력(마이크 등).</summary>
    Input
}

/// <summary>
/// 오디오 장치 하나의 정보.
/// </summary>
public sealed class AudioDeviceInfo
{
    /// <summary>장치 고유 ID(전환 시 사용).</summary>
    public required string Id { get; set; }

    /// <summary>사람이 읽는 장치명. 예: <c>Speakers (Realtek)</c></summary>
    public required string Name { get; set; }

    /// <summary>현재 기본 장치인지 여부.</summary>
    public bool IsDefault { get; set; }

    /// <summary>장치 방향(출력/입력).</summary>
    public AudioDeviceType Type { get; set; }
}
