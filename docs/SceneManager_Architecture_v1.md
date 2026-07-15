# SceneManager — 아키텍처 (현재 구현 기준)

> 개정: 2026-07. 이 문서는 **실제 코드베이스 상태**를 반영한다. 초기 설계안(전신 에이전트·위젯·
> NAudio·핫키 상주형)에서 크게 바뀌었으므로, 아래 "미구현/계획" 절에 정리한 항목은 아직 코드에 없다.

---

## 1. 제품 개념 — 편집기 + 실행기 + 바로가기

SceneManager는 상주(resident) 앱이 아니라 **세 조각**으로 나뉜다.

| 구성 | 프로젝트 | 형태 | 역할 |
|---|---|---|---|
| **씬 편집기** | `SceneManager` | WPF 앱 | 스냅샷으로 씬(JSON) 생성·관리, 배치 미리보기, 바로가기 생성 |
| **씬 실행기** | `SceneRunner` | 콘솔 없는 exe | 인자로 받은 씬 JSON을 읽어 프로그램 실행 + 창 배치 |
| **공유 코어** | `SceneManager.Core` | 클래스 라이브러리 | 위 둘이 공유하는 도메인 로직·모델·Win32 |

사용 흐름: **편집기에서 씬을 만들고 → 바탕화면 바로가기(.lnk, 인자=씬 이름)를 생성 → 아이콘을
누르면 `SceneRunner.exe`가 그 씬을 복원**한다. 항상 떠 있는 프로세스가 없다.

---

## 2. 아키텍처 원칙

| 원칙 | 설명 |
|---|---|
| **Core/UI 분리** | 도메인 로직은 전부 Core. 편집기·실행기는 Core를 호출하는 얇은 셸. |
| **인터페이스 기반** | Win32 구현을 `IDesktopManager` 등 인터페이스 뒤로 숨겨 교체·테스트 가능. |
| **설정 = 데이터** | 모든 씬은 JSON 파일. 실행 상태에 의존하지 않는 무상태(stateless) 복원. |
| **실패 허용** | 개별 프로그램 실행·배치 실패가 전체 씬 적용을 멈추지 않음(부분 실패). |
| **멱등 적용** | 이미 열린 창은 다시 실행하지 않고 위치만 재조정(창 쌓임 방지). |

---

## 3. 솔루션 구조

```
SceneManager.slnx                      # VS 2026 .slnx 포맷
│
├── src/
│   ├── SceneManager/                  # 씬 편집기 (WPF, WinExe)
│   │   ├── App.xaml(.cs)              #   합성 루트(수동 DI)
│   │   ├── Views/                     #   MainWindow, SnapshotNameDialog,
│   │   │                              #   SnapshotFineTune/Arguments/Priority Window, PromptDialog
│   │   ├── ViewModels/                #   MainViewModel + 위저드 VM(FineTune/Arguments/Priority,
│   │   │                              #   ProgramEdit/ProgramOrder 행 VM)
│   │   ├── Controls/                  #   LayoutPreview (배치 지도, 선택 강조)
│   │   ├── Services/                  #   DialogService, AppPaths, RunnerLocator, ShortcutService
│   │   └── app.manifest               #   PerMonitorV2 DPI
│   │
│   ├── SceneManager.Core/             # 공유 코어 (라이브러리)
│   │   ├── Models/                    #   데이터 모델
│   │   ├── Interfaces/                #   추상화
│   │   ├── Services/                  #   도메인 로직
│   │   ├── Persistence/               #   JSON 저장/로드
│   │   └── Platform/                  #   Win32 래퍼
│   │
│   └── SceneRunner/                   # 씬 실행기 (WinExe, AssemblyName=SceneRunner)
│       ├── Program.cs                 #   진입점: <scene-name> [--clean]
│       └── app.manifest               #   PerMonitorV2 DPI
│
├── tools/
│   └── SceneManager.DevHarness/       # 개발용 CLI(list-windows, snapshot, apply, tidy …)
│
├── tests/
│   └── SceneManager.Core.Tests/       # xUnit (DependencyResolver, ProcessFilter, LayoutTidy,
│                                      #        ArgumentAdvisor, JsonAppConfigRepository)
│
└── docs/                              # 명세서, 이 아키텍처 문서, AI 편집 프롬프트
```

- 타깃 프레임워크: **net8.0-windows** (.NET 10 SDK로 빌드).
- 프로젝트 의존성: `SceneManager ─▶ Core`, `SceneRunner ─▶ Core`, `DevHarness ─▶ Core`.
  (편집기와 실행기는 서로를 참조하지 않고 Core만 공유.)

---

## 4. Core 레이어 상세

### 4.1 Models — 데이터 모델

```csharp
public sealed class Scene
{
    public required string Id { get; set; }       // GUID
    public required string Name { get; set; }
    public string? IconPath { get; set; }
    public string? Hotkey { get; set; }           // 예약(핫키 미구현)
    public bool SquareCorners { get; set; }        // 적용 시 둥근 모서리 제거(Win11 DWM)
    public bool CloseExistingWindows { get; set; } // 실행 전 기존 창 닫기(바로가기에 --clean 부여)
    public List<ProgramEntry> Programs { get; set; } = [];
    public AudioConfig? Audio { get; set; }        // null = 오디오 안 건드림(미구현)
    public MonitorLayout? MonitorSnapshot { get; set; } // 저장 시점 모니터 구성(미사용, 계획)
    public SceneMetadata Metadata { get; set; } = new();
}

public sealed class ProgramEntry
{
    public required string Id { get; set; }        // GUID (불변 식별자)
    public required string Name { get; set; }
    public required string ExecPath { get; set; }  // 실행 파일 경로
    public string? Arguments { get; set; }         // 실행 인자(파일/URL/--new-window 등)
    public ProgramType Type { get; set; }          // Win32 | Uwp
    public string? AppUserModelId { get; set; }    // UWP 전용(AUMID)
    public int Order { get; set; }                 // 실행 순서(작을수록 먼저)
    public int DelayAfterMs { get; set; }          // 실행 후 대기
    public int SettleTimeoutMs { get; set; }       // 배치 안정화 최대 시간(0=기본 6000)
    public string? DependsOnId { get; set; }       // 선행 프로그램 id
    public bool RequiresAdmin { get; set; }        // UAC 승격 실행
    public string? WindowTitle { get; set; }       // 같은 프로세스 창 구분용 매칭 힌트
    public WindowPlacement? Window { get; set; }   // null = 실행만, 배치 없음
}

public sealed class WindowPlacement    // 좌표는 "보이는 창" 물리 px, 가상 스크린 절대 좌표
{
    public required string MonitorId { get; set; } // v0: 항상 "primary"(미사용)
    public double X, Y, Width, Height { get; set; }
    public WindowState State { get; set; }         // Normal | Maximized | Minimized
}

public enum ProgramType { Win32, Uwp }
public enum WindowState { Normal, Maximized, Minimized }
```

- **모니터 모델**: `MonitorLayout { List<MonitorInfo> }`, `MonitorInfo`(Id/DeviceName/PhysicalWidth·
  Height/PositionX·Y/IsPrimary/DpiScale …). `GetMonitorLayout()`가 채운다.
- **`ProcessFilter`**: `ShouldInclude(processName)`(화이트리스트 > 블랙리스트 > 포함) + 정적
  `CreateDefault()`(시스템/셸 프로세스 블랙리스트). 통합 설정 `app-config.json`의 `filter` 섹션에 저장된다(4.4).
- **`AppConfig`**: 전역 설정 루트(`Version` + `Filter`:ProcessFilter + `Arguments`:ArgumentConfig).
  하드코딩하던 시스템 필터·대표 프로그램 목록·힌트·인자 프롬프트를 한 파일로 모은 것(4.4의 버전 병합).
- **`AudioConfig`**: `Scene.Audio`가 참조하는 스키마 자리(현재 항상 null, 오디오 로직 미구현).

> 초기 설계의 `Scene.ClosePreviousScene`, `ProgramEntry.CloseOnSceneExit`는 **제거됨**
> (무상태 "화면 비우기"로 대체). `SettleTimeoutMs`·`WindowTitle`, 씬 옵션 `SquareCorners`·
> `CloseExistingWindows`가 추가됨.

### 4.2 Interfaces — 추상화

**활성(구현·사용 중):**

```csharp
// 데스크톱 제어(프로세스 실행 + 창 열거·배치 + 모니터). 프로세스·윈도우 매니저를 하나로 통합.
public interface IDesktopManager
{
    Task<ProcessLaunchResult> LaunchAsync(ProgramEntry entry, CancellationToken ct = default);
    List<WindowInfo> GetAllVisibleWindows();
    WindowPlacement GetPlacement(IntPtr hwnd);
    void SetPlacement(IntPtr hwnd, WindowPlacement placement);
    void CloseWindow(IntPtr hwnd);                 // WM_CLOSE(강제 종료 아님)
    void SetCornerPreference(IntPtr hwnd, bool square); // Win11 둥근/각진 모서리 전환(DWM)
    MonitorLayout GetMonitorLayout();
}

public interface ISceneRepository   // GetAll/GetById/GetByName/Save/Delete/Import/Export
public interface ISceneSnapshot     // CaptureFullAsync / CapturePartialAsync(SnapshotOptions)
public interface ISceneRunner       // ApplyAsync / ClearAsync / event ProgressChanged
```

`WindowInfo`(Handle, ProcessId, ProcessName, WindowTitle, **WindowClass**, **ExecPath?**,
**AppUserModelId?**, Placement) — 창 하나의 정보. `ProcessLaunchResult`(Success, ProcessId?, …).

> 초기 설계의 `IProcessManager`+`IWindowManager`는 **`IDesktopManager` 하나로 병합**됐다.
> `IsRunning`은 매칭 방식 변경으로 **제거**됨.

> 초기 설계의 스텁 인터페이스 `IMonitorManager`·`IAudioManager`·`IHotkeyManager`는 **제거됨**.
> 오디오·핫키·모니터 매핑은 실제 구현 시점에 새로 설계한다. (미사용 스텁이던 옛 `AppConfig`는
> 제거됐고, 지금의 `AppConfig`는 전역 설정을 담는 다른 모델이다 — 4.1/4.4 참고.)

### 4.3 Services — 도메인 로직

| 클래스 | 인터페이스 | 역할 |
|---|---|---|
| `SceneRunner` | `ISceneRunner` | 씬 적용 오케스트레이터(아래 5절) |
| `SceneSnapshot` | `ISceneSnapshot` | 현재 창들을 필터링해 씬으로 캡처 |
| `DependencyResolver` | (정적 클래스) | 의존성 위상 정렬(Kahn), 순환 감지 |
| `LayoutTidy` | (정적 클래스) | 창 사이 작은 틈·겹침을 모서리 정렬로 메움 |
| `WindowMatcher` | (정적 클래스) | 살아있는 창을 씬 프로그램에 매칭(제목·claimed). 미세조정 위저드가 사용 |
| `ArgumentAdvisor` | (인스턴스, config 주입) | 프로그램 분류(문서앱/브라우저) → 인자 힌트 + "인자 찾기" 프롬프트(한/영) 생성 |

### 4.4 Persistence · 통합 설정

- `JsonSceneRepository` (`ISceneRepository`) — `%LOCALAPPDATA%\SceneManager\scenes\*.json`, camelCase.
  파일명은 씬 **이름** 기준(`{이름}.json`).
- `JsonAppConfigRepository` — 단일 설정 파일 `app-config.json`을 로드. **버전 병합**:
  - 파일 없음 → 내장 기본값 저장(구 `process-filter.json`이 있으면 사용자 목록 이관).
  - `version` < 코드의 `AppConfig.CurrentVersion` → 관리 섹션(시스템 블랙리스트·인자 설정)만
    기본값으로 갱신하고 `userBlacklist`/`userWhitelist`는 보존해 다시 저장.
  - 버전 동일 → 파일 내용 그대로(사용자 직접 편집 존중). 한글이 그대로 보이도록 relaxed 인코더 사용.
- 통합 설정에는 필터(시스템/사용자 블랙·화이트리스트)와 인자 도우미(대표 프로그램 목록·힌트·프롬프트)가
  함께 들어간다. 하드코딩을 없애 재컴파일 없이 목록·문구를 갱신할 수 있다.

### 4.5 Platform

- `WindowsDesktopManager` (`IDesktopManager`) — 유일한 Win32 구현. `EnumWindows`, `SetWindowPos`,
  `ShowWindow`, `GetWindowPlacement`(상태), `DwmGetWindowAttribute`(cloaked + **확장 프레임 경계**),
  `DwmSetWindowAttribute`(**모서리 각짐** `SetCornerPreference`), `EnumDisplayMonitors`,
  UWP는 `explorer shell:AppsFolder\{AUMID}` 실행 + `GetApplicationUserModelId` 감지.

---

## 5. 씬 적용 플로우 (`SceneRunner.ApplyAsync`)

```
ApplyAsync(sceneId)
├─ 1. 씬 로드(ISceneRepository)
├─ 2. 같은 프로세스명 항목 수 집계(_entryCountByProcess) → 매칭 폴백 정책 결정
├─ 3. DependencyResolver.Resolve → 의존성 레벨(현재 v0는 레벨·항목 모두 순차 실행)
└─ 4. 각 ProgramEntry에 대해 ApplyProgramAsync:
      ├─ SelectWindow(processName, windowTitle, allowFallback)
      │    · 제목 정확 일치 > 부분 일치 > (허용 시) 가장 큰 창
      │    · arguments 있거나 같은 프로세스 다중 항목이면 폴백 금지
      ├─ 이미 열림?  →  아니오: LaunchAsync → WaitForWindowAsync
      │                 예:    그 창 재사용(위치만 재조정)
      ├─ _claimed에 핸들 등록(형제 항목이 같은 창 중복 배정 방지)
      └─ SettleHandleAsync: 목표 위치에 안정될 때까지 재배치(연속 2회 일치 시 성공)
           · 핸들이 죽으면(0×0, 예: 엑셀 시작창→문서창 교체) 제목으로 재선택
```

- **화면 비우기**: `ClearAsync()` — 자기 자신·시스템 프로세스를 뺀 모든 사용자 창에 `WM_CLOSE`.
  실행기에 `--clean` 인자를 주면 적용 직전에 호출(무상태, 이전 씬 개념 없음). 씬의
  `CloseExistingWindows` 옵션을 켜면 편집기가 바로가기에 `--clean`을 넣어 준다.
- **각진 모서리**: 씬 `SquareCorners`가 켜져 있으면 배치한 창마다 `SetCornerPreference(square)`로
  Win11 둥근 모서리를 없앤다(타일링 시 모서리 홈 제거).
- 상수: 폴링 200ms, 안정화 판정 연속 2회, 기본 타임아웃 6000ms, 위치 허용오차 4px.

---

## 6. 좌표 시스템 (현재 v0)

- **물리 픽셀, 가상 스크린 절대 좌표**로 저장·적용한다. 모니터와 창 좌표가 같은 공간이라
  배치 지도에 그대로 겹쳐 그릴 수 있다.
- **PerMonitorV2** 매니페스트(편집기·실행기 공통)로 OS의 좌표 가상화를 막는다.
- **DWM 확장 프레임 경계 보정**: Win32 창의 `GetWindowRect`는 비가시 리사이즈 테두리(~7px)를
  포함하므로, 캡처·조회는 `DWMWA_EXTENDED_FRAME_BOUNDS`(보이는 영역)를 쓰고, 배치 시엔 그
  여백만큼 확장해 `SetWindowPos`를 호출한다. → 저장 좌표 = 눈에 보이는 창.
- **DPI 논리 좌표 변환**은 미구현: 모델에 `DpiScale`·`LogicalWidth` 필드는 있으나 v0는 물리
  좌표만 다룬다. 서로 다른 DPI 모니터 간 이동은 아직 정밀 보정 안 함.
- **모니터 구성 변경 대응**(MonitorSnapshot 저장·비율 스케일링)은 미구현(계획).

---

## 7. UWP(Store) 앱 대응

| 항목 | Win32 | UWP |
|---|---|---|
| 실행 | `Process.Start(exe)` | `explorer.exe shell:AppsFolder\{AUMID}` |
| 감지 | 기본 | `GetApplicationUserModelId`로 AUMID 조회 성공 시 UWP |
| 경로 | `MainModule.FileName` | WindowsApps 폴더는 ACL로 직접 실행 불가 → AUMID로 활성화 |

스냅샷 시 `AppUserModelId != null`이면 `ProgramType.Uwp`로 기록하고, 실행기는 AUMID로 띄운다.

---

## 8. 편집기(WPF) 구조 — MVVM + 수동 DI

- **합성 루트** `App.xaml.cs`: `JsonSceneRepository`, `JsonAppConfigRepository`(→ `AppConfig`),
  `WindowsDesktopManager`, `SceneSnapshot`(config.Filter), `ArgumentAdvisor`(config.Arguments)를
  손으로 `new` 하여 `DialogService`·`MainViewModel`에 주입(컨테이너 없음).
- **ViewModel** `MainViewModel`: 상태(`Scenes`, `SelectedScene`, `Monitors`) + 커맨드
  (`Snapshot`, `Edit`, `Delete`, `CreateShortcut`). 실제 일은 Core/Service·다이얼로그에 위임.
- **View** `MainWindow`: 툴바(스냅샷·편집·삭제·바로가기) / 좌측 씬 목록 / 우측 상세
  (배치 지도 + 프로그램 표). `SnapshotNameDialog`는 이름 입력 모달.
- **커스텀 컨트롤** `LayoutPreview`(`Canvas` 상속 + `DependencyProperty`): 모니터·창을 비례 축소해
  그리고, `SelectedProgram`을 빨간색으로 강조(클릭 히트테스트로 양방향 선택).
- **편집기 Service**: `IDialogService`/`DialogService`(주입, 위저드·팝업 소유), `ShortcutService`·
  `AppPaths`·`RunnerLocator`(정적 유틸).
- 스택: `CommunityToolkit.Mvvm` 8.4.0(`[ObservableProperty]`, `[RelayCommand]`).

### 8.1 스냅샷/편집 위저드 (3단계, 모두 모달)

`Snapshot`은 캡처 직후(창이 살아 있음), `Edit`은 디스크에서 읽은 **복사본**으로 같은 위저드를 탄다.
`Edit`은 기존 이름·인자를 유지하고, 취소해도 원본 씬이 오염되지 않는다. **저장은 마지막 확인 때만.**

| 단계 | 창 / VM | 하는 일 |
|---|---|---|
| ① 미세조정 | `SnapshotFineTuneWindow` / `SnapshotFineTuneViewModel`(+`ProgramEditViewModel` 행) | 배치 지도 + 표에서 x/y/w/h·상태 편집 → 살아있는 창이면 `SetPlacement`로 즉시 반영. 방향키 이동/`Ctrl`+방향키 크기/`Delete` 삭제/`Ctrl+Z` 실행취소/틈 메우기. `SquareCorners`·`CloseExistingWindows` 체크박스 |
| ② 실행 인자 | `SnapshotArgumentsWindow` / `SnapshotArgumentsViewModel` | 프로그램을 하나씩 지도에 강조하며 `Arguments` 확인. 모르면 **인자 찾기 도우미**가 `ArgumentAdvisor`로 한/영 프롬프트 생성 → `PromptDialog`로 복사 |
| ③ 우선순위·의존성 | `SnapshotPriorityWindow` / `SnapshotPriorityViewModel`(+`ProgramOrderViewModel` 행) | ▲/▼로 실행 순서(`Order`) 재정렬, 행마다 의존(`DependsOnId`) 콤보, 지연(`DelayAfterMs`) 편집. 확인 시 `DependencyResolver`로 순환 검증 |

- 위저드 창 배치는 `SceneSnapshot.CaptureFullAsync`가 만든 **메모리 씬**을 직접 편집하며,
  ①에서 살아있는 창 핸들은 `WindowMatcher`가 매칭한다(같은 프로세스 다중 창은 claimed로 구분).
- `Edit`에서 이름을 바꾸면 저장 전에 이전 이름 파일을 지워 중복을 막는다(파일명 = 씬 이름).

---

## 9. 실행기(SceneRunner) 구조

- 진입점 `Program.cs`: 인자 `<scene-name> [--clean]`.
- 조립: `JsonSceneRepository` + `JsonAppConfigRepository`(→ `.Filter`) + `WindowsDesktopManager` →
  `new SceneRunner(...)`. `--clean`이면 `ClearAsync` 후 대기 → `ApplyAsync`.
- 로그: `%LOCALAPPDATA%\SceneManager\logs\runner-YYYYMMDD.log`(평문). 단계별 성공/실패 기록.
- 편집기가 만든 **바로가기(.lnk)**: TargetPath=`SceneRunner.exe`, Arguments=씬 이름
  (`ShortcutService`가 `WScript.Shell` COM으로 생성).

---

## 10. AI 씬 편집 (문서 기반)

씬 JSON을 자연어로 수정하기 위한 프롬프트가 `docs/AI_Scene_Editor_Prompt.md`에 있다. 사용자가
현재 모니터 구성 + 씬 JSON + 요청을 넣으면 AI가 수정본(또는 확인 질문)을 구조화 JSON으로 반환한다.
현재는 **수동 복붙** 워크플로이며, 편집기 내 API 연동은 계획(2차).

또한 위저드 ②단계의 **인자 찾기 도우미**는 프로그램 정보를 채운 짧은 프롬프트(한/영)를 `ArgumentAdvisor`가
생성해 `PromptDialog`로 복사하게 해 준다("이 앱을 특정 파일/URL로 열 인자와 그 경로 확인법"). 프롬프트
문구·대표 프로그램 목록은 `app-config.json`에서 오므로 코드 수정 없이 바꿀 수 있다.

---

## 11. 의존성 그래프 실행

`DependencyResolver.Resolve(programs)`(정적) — `DependsOnId`를 DAG로 보고 **Kahn 위상 정렬**로
실행 레벨을 만든다. 같은 레벨은 병렬 가능하다고 표시하지만 **v0는 순차 실행**한다. 순환이면
`CircularDependencyException`. 같은 레벨 내부는 `Order` 오름차순.

---

## 12. 로깅

- **실행기**: 평문 파일 로그(위 9절). 구조화 로깅 라이브러리 미도입.
- **편집기**: 사용자 알림은 다이얼로그(`DialogService`). 별도 파일 로그 없음.

---

## 13. 외부 의존성 (NuGet)

| 패키지 | 버전 | 용도 |
|---|---|---|
| CommunityToolkit.Mvvm | 8.4.0 | 편집기 MVVM 도우미 |

> 초기 설계의 NAudio(오디오)·Serilog(로깅)·Hardcodet.NotifyIcon(트레이)은 **아직 도입 안 함**.

---

## 14. 미구현 / 계획

| 항목 | 상태 |
|---|---|
| 오디오(마스터/장치/앱별 볼륨) | 모델·스텁 인터페이스만. 로직 없음 |
| 글로벌 핫키 | `Scene.Hotkey` 필드·스텁만 |
| 모니터 매핑·해상도 스케일링·`MonitorSnapshot` 저장 | 미구현 |
| DPI 논리 좌표 변환 | 미구현(물리 px만) |
| 경로 유효성 검증(실행 전) | 미구현 |
| 관리자 권한 헬퍼 프로세스 분리 | 현재는 `Verb=runas` 직접 실행 |
| 인스톨러 / 포터블 배포 / CI | 미구성 |
| 트레이 아이콘·바탕화면 위젯 | 폐기(바로가기 모델로 대체) |
| 씬 편집 화면 | **구현됨** — 스냅샷과 동일한 3단계 위저드 재사용(8.1). 프로그램 개별 편집 화면은 별도 계획 |
| AI 편집 편집기 내 연동 | 계획(현재는 수동 복붙 + 인자 찾기 도우미 프롬프트 생성) |
```
