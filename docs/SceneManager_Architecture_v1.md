# SceneManager — 아키텍처 설계서 v1.0

---

## 1. 아키텍처 원칙

| 원칙 | 설명 |
|------|------|
| **Core/UI 분리** | 모든 비즈니스 로직은 Core 라이브러리 구현하고, GUI는 Core를 호출하는 얇은 셸 형식으로 구현한다. |
| **인터페이스 기반 설계** | Win32/UWP, NAudio/대체 구현 등 교체 가능하도록 인터페이스 추상화한다. |
| **최소 권한** | 일반 권한으로 동작할 수 있게 한다. 관리자 권한은 별도 헬퍼 프로세스로 분리 |
| **설정 = 데이터** | 모든 씬/설정은 JSON 파일. 프로그램 상태에 의존하지 않음 |
| **실패 허용** | 개별 프로그램 실행/배치 실패가 전체 씬 적용을 중단시키지 않음 |

---

## 2. 솔루션 구조

```
SceneManager.sln
│
├── src/
│   ├── SceneManager.Core/              # 핵심 비즈니스 로직 (클래스 라이브러리)
│   │   ├── Models/                     # 데이터 모델
│   │   ├── Interfaces/                 # 추상화 인터페이스
│   │   ├── Services/                   # 서비스 구현
│   │   ├── Platform/                   # Windows API 래퍼
│   │   └── Utils/                      # 유틸리티
│   │
│   └── SceneManager.Gui/              # GUI 프론트엔드 (WPF 앱)
│       ├── Views/
│       ├── ViewModels/
│       ├── Widgets/                    # 바탕화면 위젯
│       └── Resources/
│
├── tests/
│   ├── SceneManager.Core.Tests/
│   └── SceneManager.Integration.Tests/
│
├── docs/
│   ├── spec-v1.md
│   └── architecture-v1.md
│
├── assets/                             # 아이콘, 이미지
├── installer/                          # Inno Setup 스크립트
└── .github/workflows/                  # CI/CD
```

### 프로젝트 의존성 방향

```
SceneManager.Gui ──▶ SceneManager.Core

(GUI는 Core만 참조.)
```

---

## 3. Core 레이어 상세 설계

### 3.1 Models — 데이터 모델

```csharp
// ── 씬 루트 ──
public class Scene
{
    public string Id { get; set; }              // GUID
    public string Name { get; set; }            // "업무", "게임"
    public string? IconPath { get; set; }
    public string? Hotkey { get; set; }          // "Ctrl+Alt+1"
    public bool ClosePreviousScene { get; set; } // default: true
    public List<ProgramEntry> Programs { get; set; }
    public AudioConfig Audio { get; set; }
    public MonitorLayout MonitorSnapshot { get; set; }
    public SceneMetadata Metadata { get; set; }
}

public class SceneMetadata
{
    public string SchemaVersion { get; set; }   // "1.0"
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}

// ── 프로그램 엔트리 ──
public class ProgramEntry
{
    public string Id { get; set; }
    public string Name { get; set; }            // 표시명
    public string ExecPath { get; set; }        // 실행 경로
    public string? Arguments { get; set; }      // 실행 인자
    public ProgramType Type { get; set; }       // Win32 | UWP
    public string? AppUserModelId { get; set; } // UWP 전용 (AUMID)
    public int Order { get; set; }              // 실행 순서
    public int DelayAfterMs { get; set; }       // 실행 후 대기(ms)
    public string? DependsOnId { get; set; }    // 의존 프로그램 ID
    public bool CloseOnSceneExit { get; set; }  // 씬 전환 시 종료 여부
    public WindowPlacement? Window { get; set; }
}

public enum ProgramType
{
    Win32,
    Uwp
}

// ── 윈도우 배치 (논리 픽셀 기준) ──
public class WindowPlacement
{
    public string MonitorId { get; set; }       // 모니터 식별자
    public double X { get; set; }               // 논리 픽셀
    public double Y { get; set; }               // 논리 픽셀
    public double Width { get; set; }           // 논리 픽셀
    public double Height { get; set; }          // 논리 픽셀
    public WindowState State { get; set; }      // Normal | Maximized | Minimized
}

public enum WindowState
{
    Normal,
    Maximized,
    Minimized
}

// ── 모니터 레이아웃 ──
public class MonitorLayout
{
    public List<MonitorInfo> Monitors { get; set; }
}

public class MonitorInfo
{
    public string Id { get; set; }              // DeviceName or 고유 식별자
    public string DeviceName { get; set; }      // \\.\DISPLAY1
    public int PhysicalWidth { get; set; }      // 물리 해상도
    public int PhysicalHeight { get; set; }
    public double LogicalWidth { get; set; }    // 논리 해상도
    public double LogicalHeight { get; set; }
    public int DpiScale { get; set; }           // DPI 스케일 (100, 125, 150 ...)
    public double PositionX { get; set; }       // 논리 좌표계 내 위치
    public double PositionY { get; set; }
    public bool IsPrimary { get; set; }
}

// ── 오디오 설정 ──
public class AudioConfig
{
    public int MasterVolume { get; set; }       // 0-100
    public string? DefaultOutputDevice { get; set; }  // 장치명 or ID
    public string? DefaultInputDevice { get; set; }
    public int InputVolume { get; set; }        // 0-100
    public List<AppVolumeEntry> AppVolumes { get; set; }
}

public class AppVolumeEntry
{
    public string ProcessName { get; set; }     // "chrome.exe"
    public int Volume { get; set; }             // 0-100
}

// ── 프로세스 필터 ──
public class ProcessFilter
{
    public List<string> SystemBlacklist { get; set; }   // 내장 (svchost 등)
    public List<string> UserBlacklist { get; set; }     // 사용자 추가
    public List<string> UserWhitelist { get; set; }     // 명시적 포함
}

// ── 글로벌 설정 ──
public class AppConfig
{
    public bool RunOnStartup { get; set; }
    public bool ShowWidget { get; set; }
    public WidgetPosition WidgetPosition { get; set; }
    public double WidgetOpacity { get; set; }
    public string LogLevel { get; set; }        // "Info", "Debug", "Error"
    public string ScenesDirectory { get; set; }
}
```

### 3.2 Interfaces — 추상화 계층

```csharp
// ── 프로세스 관리 ──
public interface IProcessManager
{
    /// 현재 실행 중인 UI 프로세스 목록 (스냅샷용)
    Task<List<RunningProcessInfo>> GetRunningProcessesAsync();

    /// 프로그램 실행 (Win32 + UWP 통합)
    Task<ProcessLaunchResult> LaunchAsync(ProgramEntry entry);

    /// 프로그램 종료 (Graceful → Force)
    Task<bool> CloseAsync(ProgramEntry entry, int gracefulTimeoutMs = 5000);

    /// 특정 프로세스가 실행 중인지 확인
    bool IsRunning(string processName);
}

public class RunningProcessInfo
{
    public string ProcessName { get; set; }
    public string? ExecPath { get; set; }
    public int ProcessId { get; set; }
    public ProgramType Type { get; set; }
    public string? AppUserModelId { get; set; }  // UWP
    public IntPtr MainWindowHandle { get; set; }
    public string? WindowTitle { get; set; }
}

public class ProcessLaunchResult
{
    public bool Success { get; set; }
    public int? ProcessId { get; set; }
    public IntPtr? WindowHandle { get; set; }
    public string? ErrorMessage { get; set; }
}

// ── 윈도우 관리 ──
public interface IWindowManager
{
    /// 특정 프로세스의 메인 윈도우 핸들 대기
    Task<IntPtr> WaitForWindowAsync(int processId, int timeoutMs = 10000);

    /// 현재 윈도우 배치 정보 읽기 (논리 픽셀)
    WindowPlacement GetPlacement(IntPtr hwnd);

    /// 윈도우 배치 적용 (논리 픽셀 → 물리 픽셀 변환 내부 처리)
    void SetPlacement(IntPtr hwnd, WindowPlacement placement);

    /// 모든 보이는 윈도우의 배치 정보
    List<WindowInfo> GetAllVisibleWindows();
}

public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public int ProcessId { get; set; }
    public string ProcessName { get; set; }
    public string WindowTitle { get; set; }
    public WindowPlacement Placement { get; set; }
}

// ── 모니터 관리 ──
public interface IMonitorManager
{
    /// 현재 연결된 모니터 정보
    MonitorLayout GetCurrentLayout();

    /// 저장된 레이아웃과 현재 레이아웃 비교
    MonitorMappingResult MapMonitors(MonitorLayout saved, MonitorLayout current);

    /// 논리 좌표 ↔ 물리 좌표 변환
    PhysicalRect LogicalToPhysical(double x, double y, double w, double h, MonitorInfo monitor);
    LogicalRect PhysicalToLogical(int x, int y, int w, int h, MonitorInfo monitor);
}

public class MonitorMappingResult
{
    public bool IsExactMatch { get; set; }
    public Dictionary<string, string> MonitorIdMap { get; set; }  // saved → current
    public List<string> UnmappedSavedMonitors { get; set; }
    public List<string> Warnings { get; set; }
}

// ── 오디오 관리 ──
public interface IAudioManager
{
    /// 현재 오디오 상태 스냅샷
    AudioConfig GetCurrentConfig();

    /// 오디오 설정 적용
    Task ApplyConfigAsync(AudioConfig config);

    /// 마스터 볼륨
    int GetMasterVolume();
    void SetMasterVolume(int volume);

    /// 기본 장치 전환
    List<AudioDeviceInfo> GetOutputDevices();
    List<AudioDeviceInfo> GetInputDevices();
    void SetDefaultOutputDevice(string deviceId);
    void SetDefaultInputDevice(string deviceId);

    /// 앱별 볼륨
    void SetAppVolume(string processName, int volume);
    int? GetAppVolume(string processName);
}

public class AudioDeviceInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool IsDefault { get; set; }
    public AudioDeviceType Type { get; set; }
}

// ── 단축키 관리 ──
public interface IHotkeyManager
{
    /// 핫키 등록
    HotkeyRegistrationResult Register(string hotkeyString, Action callback);

    /// 핫키 해제
    void Unregister(string hotkeyString);

    /// 모든 핫키 해제
    void UnregisterAll();

    /// 충돌 검사
    bool IsConflicting(string hotkeyString);
}

public class HotkeyRegistrationResult
{
    public bool Success { get; set; }
    public string? ConflictWith { get; set; }  // 충돌 대상
    public string? ErrorMessage { get; set; }
}

// ── 씬 저장/로드 ──
public interface ISceneRepository
{
    Task<List<Scene>> GetAllAsync();
    Task<Scene?> GetByIdAsync(string id);
    Task<Scene?> GetByNameAsync(string name);
    Task SaveAsync(Scene scene);
    Task DeleteAsync(string id);
    Task<Scene> ImportAsync(string filePath);
    Task ExportAsync(string id, string filePath);
}

// ── 스냅샷 서비스 ──
public interface ISnapshotService
{
    /// 전체 스냅샷 캡처
    Task<Scene> CaptureFullAsync(string sceneName);

    /// 부분 스냅샷 캡처
    Task<Scene> CapturePartialAsync(string sceneName, SnapshotOptions options);
}

public class SnapshotOptions
{
    public bool CapturePrograms { get; set; } = true;
    public bool CaptureWindowPlacement { get; set; } = true;
    public bool CaptureAudio { get; set; } = true;
}
```

### 3.3 Services — 핵심 서비스

```csharp
// ── 씬 엔진: 씬 적용의 오케스트레이터 ──
public interface ISceneEngine
{
    /// 씬 적용 (전체 플로우)
    Task<SceneApplyResult> ApplyAsync(string sceneId, CancellationToken ct = default);

    /// 현재 적용 중인 씬
    string? CurrentSceneId { get; }

    /// 진행 상황 이벤트
    event EventHandler<SceneProgressEventArgs> ProgressChanged;
}

public class SceneApplyResult
{
    public bool Success { get; set; }
    public string SceneId { get; set; }
    public TimeSpan Elapsed { get; set; }
    public List<StepResult> Steps { get; set; }
}

public class StepResult
{
    public string StepName { get; set; }       // "Launch Chrome", "Set Audio"
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SceneProgressEventArgs : EventArgs
{
    public string StepDescription { get; set; }
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public double ProgressPercent { get; set; }
}
```

### 3.4 씬 적용 플로우 (SceneEngine 내부)

```
ApplyAsync(sceneId)
│
├── 1. 씬 로드 (ISceneRepository)
│
├── 2. 실행 전 검증
│   ├── 프로그램 경로 유효성 검사
│   ├── 모니터 구성 비교 (IMonitorManager.MapMonitors)
│   └── 오디오 장치 존재 확인
│       └── 검증 실패 항목은 경고 로그 + 스킵 (전체 중단 아님)
│
├── 3. 이전 씬 정리 (ClosePreviousScene == true 일 때)
│   ├── 이전 씬의 프로그램 중 CloseOnSceneExit == true 인 것들 종료
│   └── Graceful(WM_CLOSE) → 타임아웃 → Force(Kill)
│
├── 4. 프로그램 실행 (의존성 그래프 순서)
│   ├── 의존성 그래프를 위상 정렬(Topological Sort)
│   ├── 각 프로그램:
│   │   ├── IProcessManager.LaunchAsync()
│   │   ├── IWindowManager.WaitForWindowAsync()
│   │   ├── IWindowManager.SetPlacement()    // 논리→물리 변환 내부 처리
│   │   └── DelayAfterMs 대기
│   └── 실패 시: 로그 + 다음 프로그램 계속
│
├── 5. 오디오 적용
│   ├── 기본 출력/입력 장치 전환
│   ├── 마스터 볼륨 설정
│   └── 앱별 볼륨 설정 (프로세스 실행 완료 후)
│
└── 6. 결과 반환 + 로그 기록
```

---

## 4. 좌표 시스템 설계 (논리 픽셀)

### 4.1 저장 원칙

```
저장: 항상 논리 픽셀 (DPI 스케일 적용된 좌표)
적용: 논리 → 물리 변환 후 Win32 API 호출
읽기: 물리 → 논리 변환 후 저장
```

### 4.2 변환 공식

```csharp
// 물리 → 논리 (스냅샷 캡처 시)
logicalX = physicalX / (dpiScale / 100.0)
logicalY = physicalY / (dpiScale / 100.0)

// 논리 → 물리 (씬 적용 시)
physicalX = logicalX * (dpiScale / 100.0)
physicalY = logicalY * (dpiScale / 100.0)
```

### 4.3 모니터 해상도 변경 대응

```
씬 저장 시:
  - 각 모니터의 논리 해상도, DPI, 위치를 MonitorLayout에 저장
  - 윈도우 좌표는 해당 모니터 기준 상대 좌표가 아닌 전체 가상 스크린 기준 절대 좌표

씬 적용 시:
  1. 현재 모니터 레이아웃 조회
  2. 저장된 레이아웃과 비교 (IMonitorManager.MapMonitors)
  3. 매핑 전략:
     a. 모니터 수 동일 + 해상도 동일 → 그대로 적용
     b. 모니터 수 동일 + 해상도 다름 → 비율 스케일링
        scaledX = savedX * (currentLogicalWidth / savedLogicalWidth)
        scaledY = savedY * (currentLogicalHeight / savedLogicalHeight)
     c. 모니터 수 다름 → 주 모니터에 집중 배치 + 경고
```

### 4.4 해상도 비율 스케일링 예시

```
저장 환경: 모니터0 = 1920x1080 (100% DPI)
적용 환경: 모니터0 = 2560x1440 (100% DPI)

저장된 윈도우: x=960, y=0, w=960, h=1080
스케일링:
  x = 960 * (2560/1920) = 1280
  y = 0   * (1440/1080) = 0
  w = 960 * (2560/1920) = 1280
  h = 1080* (1440/1080) = 1440

결과: 화면 오른쪽 절반에 최대화 → 비율 유지됨
```

---

## 5. UWP 앱 대응 설계

### 5.1 Win32 vs UWP 차이점

| 항목 | Win32 | UWP (Store 앱) |
|------|-------|-----------------|
| 실행 | exe 경로로 Process.Start | shell:AppsFolder + AUMID |
| 프로세스명 | 고유 exe 이름 | ApplicationFrameHost.exe (공유) |
| 윈도우 핸들 | 직접 접근 가능 | ApplicationFrameWindow 래퍼 통과 |
| 오디오 세션 | 프로세스명으로 매칭 | 별도 매핑 필요 |

### 5.2 UWP 실행 방법

```csharp
// UWP 앱 실행: AUMID (Application User Model ID) 사용
// 예: Netflix → "4DF9E0F8.Netflix_mcm4njqhnhss8!Netflix.App"

public async Task<ProcessLaunchResult> LaunchUwpAsync(string aumid)
{
    var process = Process.Start(new ProcessStartInfo
    {
        FileName = "explorer.exe",
        Arguments = $"shell:AppsFolder\\{aumid}"
    });

    // 또는 IApplicationActivationManager COM 인터페이스 사용 (더 정밀)
}
```

### 5.3 UWP 윈도우 식별

```csharp
// UWP 앱은 ApplicationFrameHost.exe가 호스팅
// 실제 앱을 식별하려면 윈도우의 자식 프로세스(CoreWindow)를 확인

// EnumWindows → 클래스명 "ApplicationFrameWindow" 필터
// → DwmGetWindowAttribute 또는 자식 윈도우의 실제 PID 확인
```

### 5.4 스냅샷 시 UWP 감지

```csharp
// 스냅샷 캡처 시 프로세스 유형 자동 판별:
// 1. exe 경로가 WindowsApps 폴더 하위 → UWP
// 2. 프로세스명이 ApplicationFrameHost.exe → UWP (윈도우별 실제 앱 식별 필요)
// 3. Package.appxmanifest 존재 여부로 확인
```

---

## 6. GUI 설계 (WPF)

### 7.1 화면 구성

```
┌─────────────────────────────────────────────────┐
│  SceneManager                          ─ □ ✕    │
├─────────┬───────────────────────────────────────┤
│         │                                       │
│  씬 목록 │   메인 콘텐츠 영역                     │
│         │                                       │
│ ┌─────┐ │   [씬 상세 / 편집]                     │
│ │ 업무 │ │   ┌──────────────────────────────┐    │
│ └─────┘ │   │ 프로그램 탭 │ 배치 탭 │ 사운드 탭 │    │
│ ┌─────┐ │   ├──────────────────────────────┤    │
│ │ 게임 │ │   │                              │    │
│ └─────┘ │   │   (탭별 편집 UI)              │    │
│ ┌──────┐│   │                              │    │
│ │넷플릭스││   │                              │    │
│ └──────┘│   └──────────────────────────────┘    │
│         │                                       │
│ [+ 새 씬]│   [스냅샷 캡처]  [씬 적용]  [Export]   │
│         │                                       │
├─────────┴───────────────────────────────────────┤
│  상태바: 현재 씬: 업무 | 마지막 적용: 10:30      │
└─────────────────────────────────────────────────┘
```

### 7.2 바탕화면 위젯

```
┌──────────────────┐
│  SceneManager    │ ← 드래그 영역
├──────────────────┤
│  ▶ 업무          │ ← 클릭 = 씬 적용
│  ▶ 게임          │
│  ▶ 넷플릭스      │
├──────────────────┤
│  ⚙ 설정          │
└──────────────────┘

- TopMost, 반투명 (Opacity 조절 가능)
- 마우스 이탈 시 축소 (아이콘만 표시)
- 마우스 호버 시 확장
```

### 7.3 MVVM 구조

```
Views/
├── MainWindow.xaml              # 메인 설정 창
├── SceneEditorView.xaml         # 씬 편집
├── SnapshotWizardView.xaml      # 스냅샷 캡처 위자드
├── ProcessFilterView.xaml       # 필터 편집
├── SettingsView.xaml            # 글로벌 설정
└── LogViewerView.xaml           # 로그 뷰어

ViewModels/
├── MainViewModel.cs
├── SceneListViewModel.cs
├── SceneEditorViewModel.cs
├── SnapshotWizardViewModel.cs
├── ProcessFilterViewModel.cs
├── SettingsViewModel.cs
└── LogViewerViewModel.cs

Widgets/
├── DesktopWidget.xaml           # 바탕화면 위젯
└── DesktopWidgetViewModel.cs
```

---

## 7. DI (의존성 주입) 구성

```csharp
// Core 서비스 등록 (GUI 등 공용)
public static class ServiceRegistration
{
    public static IServiceCollection AddSceneManagerCore(
        this IServiceCollection services)
    {
        // Platform
        services.AddSingleton<IProcessManager, WindowsProcessManager>();
        services.AddSingleton<IWindowManager, WindowsWindowManager>();
        services.AddSingleton<IMonitorManager, WindowsMonitorManager>();
        services.AddSingleton<IAudioManager, NAudioAudioManager>();
        services.AddSingleton<IHotkeyManager, Win32HotkeyManager>();

        // Services
        services.AddSingleton<ISceneEngine, SceneEngine>();
        services.AddSingleton<ISnapshotService, SnapshotService>();
        services.AddSingleton<ISceneRepository, JsonSceneRepository>();

        // Utils
        services.AddSingleton<ProcessFilter>();
        services.AddSingleton<ILogger>(sp =>
            new LoggerConfiguration()
                .WriteTo.File("logs/sceneman-.log",
                    rollingInterval: RollingInterval.Day)
                .CreateLogger());

        return services;
    }
}

// GUI에서 사용
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();
        services.AddSceneManagerCore();
        services.AddSingleton<MainViewModel>();
        // ...
    }
}
```

---

## 8. 의존성 그래프 실행 (위상 정렬)

```csharp
// 프로그램 간 의존성을 DAG로 모델링, 위상 정렬로 실행 순서 결정

public class DependencyResolver
{
    /// 의존성 순서대로 실행 그룹 반환
    /// 같은 그룹 내 프로그램은 병렬 실행 가능
    public List<List<ProgramEntry>> Resolve(List<ProgramEntry> programs)
    {
        // 1. 인접 리스트 구성
        // 2. 위상 정렬 (Kahn's algorithm)
        // 3. 순환 의존성 감지 → 에러
        // 4. 같은 depth의 노드를 그룹으로 묶어 반환
    }
}

// 실행 예시:
// VPN(order:1) → 사내앱(depends:VPN) → 사내메신저(depends:사내앱)
// Chrome(order:1, 독립)
//
// 결과:
// Group 0: [VPN, Chrome]    ← 동시 실행 가능
// Group 1: [사내앱]          ← VPN 완료 후
// Group 2: [사내메신저]      ← 사내앱 완료 후
```

---

## 9. 로깅 설계

### 9.1 로그 레벨

| 레벨 | 용도 |
|------|------|
| Debug | 개발 디버깅용 상세 정보 |
| Info | 씬 적용/전환, 프로그램 실행 성공 |
| Warning | 경로 무효, 모니터 불일치, 장치 없음 |
| Error | 프로그램 실행 실패, 윈도우 배치 실패 |

### 9.2 로그 포맷

```
[2026-07-12 10:30:15.123] [INFO]  SceneEngine  | Applying scene: 업무 (id: abc-123)
[2026-07-12 10:30:15.200] [INFO]  ProcessMgr   | Launching: Chrome (C:\...\chrome.exe)
[2026-07-12 10:30:16.500] [INFO]  WindowMgr    | Window found for Chrome (PID: 1234, hWnd: 0x1A2B)
[2026-07-12 10:30:16.510] [INFO]  WindowMgr    | Placement applied: Monitor0, (0,0,1920,1080), Maximized
[2026-07-12 10:30:16.600] [WARN]  AudioMgr     | App volume target not found: slack.exe (not running yet)
[2026-07-12 10:30:18.000] [INFO]  SceneEngine  | Scene applied: 업무 (elapsed: 2.877s, 5/5 steps OK)
```

### 9.3 로그 파일 관리

```
logs/
├── sceneman-2026-07-12.log
├── sceneman-2026-07-11.log
└── ...

- 일별 롤링
- 보관 기간: 30일 (설정 가능)
- 최대 파일 크기: 10MB/일
```

---

## 10. 관리자 권한 설계

### 10.1 원칙
- SceneManager 자체는 **일반 권한**으로 실행
- 관리자 권한이 필요한 프로그램만 별도 elevated 프로세스로 실행

### 10.2 구현 방식

```csharp
// 관리자 권한 필요 프로그램 실행
var psi = new ProcessStartInfo
{
    FileName = entry.ExecPath,
    Arguments = entry.Arguments,
    Verb = "runas",          // UAC 프롬프트 발생
    UseShellExecute = true   // runas에 필요
};

try
{
    Process.Start(psi);
}
catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
{
    // 사용자가 UAC 거부함
    _logger.Warning("UAC denied for {Program}", entry.Name);
}
```

### 10.3 ProgramEntry 확장

```csharp
public class ProgramEntry
{
    // ... 기존 필드 ...
    public bool RequiresAdmin { get; set; }  // 관리자 권한 필요 여부
}
```

---

## 11. 외부 의존성 (NuGet)

| 패키지 | 버전 | 용도 | 라이선스 |
|--------|------|------|----------|
| NAudio | 2.x | Core Audio API (볼륨, 장치) | MIT |
| Serilog | 3.x | 구조화 로깅 | Apache 2.0 |
| Serilog.Sinks.File | 5.x | 파일 로그 출력 | Apache 2.0 |
| CommunityToolkit.Mvvm | 8.x | MVVM 도우미 (WPF) | MIT |
| Hardcodet.NotifyIcon.Wpf | 1.x | 시스템 트레이 아이콘 | CPOL → MIT 대안 검토 |

---

## 12. 빌드 및 배포

### 12.1 빌드 구성

```yaml
# .github/workflows/build.yml
name: Build & Release
on:
  push:
    tags: ['v*']

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet build -c Release
      - run: dotnet test
      - run: dotnet publish src/SceneManager.Gui -c Release -o publish/gui
      # Installer 빌드 + Release 업로드
```

### 12.2 배포 형태

| 형태 | 대상 | 포함 내용 |
|------|------|-----------|
| Installer (.exe) | 일반 사용자 | GUI + 시작메뉴 등록 |
| Portable (.zip) | 파워유저 | GUI, 설치 불필요 |

---

## 부록: 파일별 구현 우선순위

### 즉시 착수 (뼈대)
1. `SceneManager.Core/Models/` — 전체 모델 클래스
2. `SceneManager.Core/Interfaces/` — 전체 인터페이스
3. `SceneManager.Core/Services/SceneEngine.cs` — 씬 적용 오케스트레이터
4. `SceneManager.Core/Platform/WindowsProcessManager.cs` — 프로세스 실행
5. `SceneManager.Core/Platform/WindowsWindowManager.cs` — 윈도우 배치

### 그 다음
6. `SceneManager.Core/Platform/WindowsMonitorManager.cs` — 모니터 + 좌표 변환
7. `SceneManager.Core/Platform/NAudioAudioManager.cs` — 사운드 제어
8. `SceneManager.Core/Services/SnapshotService.cs` — 스냅샷 캡처
9. `SceneManager.Core/Services/JsonSceneRepository.cs` — JSON 저장/로드

### GUI
10. `SceneManager.Gui/` — WPF 메인 윈도우 + 위젯
