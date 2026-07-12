# SceneManager (씬 매니저) — 프로젝트 명세서 v1.0

> 버튼 하나로 데스크탑 환경을 원하는 상태로 자동 구성하는 Windows 유틸리티
> 오픈소스 배포를 전제로 설계

---

## 1. 프로젝트 개요

### 1.1 핵심 컨셉
- 사용자가 "씬(Scene)"을 정의하면, 원클릭/단축키로 데스크탑 환경을 해당 씬으로 전환
- 씬 = 프로그램 실행 + 화면 배치 + 사운드 설정의 조합
- 설정 방법: 현재 PC 상태를 스냅샷으로 캡처 → 편집 → 저장

### 1.2 대상 사용자
- 업무/게임/미디어 등 용도별로 데스크탑 환경을 자주 전환하는 사용자
- 멀티모니터 사용자
- PC방 스타일의 빠른 환경 전환을 원하는 사용자

---

## 2. 기능 명세

| No | 항목 | 기능 이름 | 기능 설명 |
|----|------|-----------|-----------|
| 1 | 씬 관리 | 씬 생성 | 스냅샷 캡처를 할 수 있다. 또는 수동 구성으로 새 씬 생성할 수 있다. |
| 2 | 씬 관리 | 씬 편집 | 만들어진 씬의 개별 항목(프로그램/배치/사운드)을 수정하고·삭제하거나·추가할 수 있다. |
| 3 | 씬 관리 | 씬 삭제 | 만들어진 스냅샷을 삭제할 수 있다. |
| 4 | 씬 관리 | 씬 복제 | 기존 씬을 기반으로 새 씬 복제할 수 있다. |
| 5 | 씬 관리 | 씬 Import/Export | JSON 파일 단위로 씬을 내보내기/불러오기 할 수 있다. |
| 6 | 프로그램 실행 | 순차 실행 | 씬에 등록된 프로그램을 정해진 순서에 맞춰 실행할 수 있다. |
| 7 | 프로그램 실행 | 실행 정보 저장 | 프로그램별 실행 경로(exe path)와 실행 인자(arguments) 저장한다. |
| 8 | 프로그램 실행 | 경로 유효성 검증 | 실행 전 경로 검증, 무효 시 사용자 알림 후 해당 항목 스킵할 수 있다. |
| 9 | 프로그램 실행 | 의존성 설정 | 프로그램 간 선후관계에 따라 실행될 수 있다. |
| 10 | 프로그램 실행 | 딜레이 설정 | 프로그램별 실행 후 대기 시간(ms) 설정할 수 있다. |
| 11 | 프로그램 실행 | 실행 순서 편집 | 드래그&드롭 또는 순서 번호로 실행 순서 지정할 수 있다. |
| 12 | 프로그램 실행 | 이전 프로그램 종료 | 씬 전환 시 이전 씬 프로그램 자동 종료를 옵션으로 선택할 수 있다. (기본값 ON) |
| 13 | 프로그램 실행 | 종료 옵션 | 종료하지 않음 / 선택적 종료 (프로그램별 종료 여부 플래그) |
| 14 | 프로그램 실행 | Graceful 종료 | WM_CLOSE 우선, 타임아웃 후 강제 종료(Kill) |
| 15 | 프로그램 실행 | 실패 재시도 `[후순위]` | 실행 실패 시 최대 3회 재시도, 간격 설정, 최종 실패 시 로그 기록·사용자 알림 |
| 16 | 프로그램 실행 | 중복 실행 방지 `[후순위]` | 실행 중 프로세스 감지, 재활용 / 종료 후 재실행 / 스킵 옵션 |
| 17 | 화면 배치 | 윈도우 위치·크기 저장 | x·y 좌표와 width·height 저장 |
| 18 | 화면 배치 | 윈도우 상태 저장 | 최대화 / 최소화 / 일반 상태 저장 |
| 19 | 화면 배치 | 대상 모니터 저장 | 모니터 인덱스 또는 식별자 저장 |
| 20 | 화면 배치 | 멀티모니터 대응 | 모니터별 해상도·위치 저장, 적용 시 현재 구성과 비교, 불일치 시 경고 표시 + 최선의 매핑 시도 |
| 21 | 화면 배치 | 윈도우 복원 타이밍 | 프로세스 실행 후 윈도우 핸들 생성 폴링, 타임아웃 시 스킵 + 로그 |
| 22 | 화면 배치 | DPI 스케일링 `[검토 필요]` | 모니터별 DPI 상이 시 좌표 보정, 물리/논리 픽셀 기준 결정 필요 |
| 23 | 사운드 제어 | 앱별 볼륨 | Windows 볼륨 믹서 기준 앱별 볼륨 레벨 저장/복원 |
| 24 | 사운드 제어 | 마스터 볼륨 | 마스터 볼륨 레벨 저장/복원 |
| 25 | 사운드 제어 | 출력 장치 전환 | 기본 출력 장치 전환 (예: 스피커 ↔ 헤드셋) |
| 26 | 사운드 제어 | 앱별 출력 장치 | 앱별 출력 장치 지정 (Windows 10 1803+ "앱 볼륨 및 장치 기본 설정" 활용) |
| 27 | 사운드 제어 | 입력 장치 전환 | 기본 입력 장치(마이크) 전환 |
| 28 | 사운드 제어 | 입력 볼륨 | 입력 볼륨 레벨 저장/복원 |
| 29 | 스냅샷 | 전체 스냅샷 캡처 | 실행 중인 프로그램·화면 배치·사운드 설정을 한 번에 캡처 |
| 30 | 스냅샷 | 부분 스냅샷 캡처 | 캡처 대상 선택 (프로그램만 / 배치만 / 사운드만 / 조합) |
| 31 | 스냅샷 | 프로세스 필터링 | UI가 있는 프로세스만 캡처, 시스템 프로세스 블랙리스트 내장(svchost, explorer, dwm 등) |
| 32 | 스냅샷 | 필터 사용자 편집 | 사용자 정의 블랙리스트/화이트리스트 편집 |
| 33 | 스냅샷 | 스냅샷 편집 | 캡처 결과 개별 항목 추가·수정·삭제, 실행 순서·딜레이·의존성 설정 |

> **스냅샷 플로우**: 스냅샷 캡처 → 프로세스 필터링 → 스냅샷 편집 → 실행 순서/의존성 편집 → 씬으로 저장

---

## 3. UI/UX 명세

### 3.1 바탕화면 위젯 (피씨방 스타일)
- 바탕화면에 항상 표시되는 컨트롤러 패널
- 씬 목록 + 원클릭 실행 버튼
- 옵션으로 안띄울 수 있다.

### 3.2 글로벌 단축키
- 씬별 단축키 할당 (예: Ctrl+Alt+1 = 업무, Ctrl+Alt+2 = 게임)
- 단축키 충돌 감지: 등록 시 기존 단축키와 충돌 검사
- 사용자 정의 단축키 편집 UI

### 3.3 메인 설정 창
- 씬 관리: 목록, 생성, 편집, 삭제, 복제
- 스냅샷 캡처 + 편집 위자드
- 글로벌 설정: 자동 실행, 단축키, 위젯 설정, 로그 뷰어
- 프로세스 필터 관리 (블랙리스트/화이트리스트)

### 3.4 실행 프로세스
- 정해진 스냅샷을 실행하는 프로세스.

---

## 4. 데이터 설계

### 4.1 저장 포맷
- **JSON** 기반
- 씬별 개별 파일 또는 단일 파일 내 배열 (TBD)
- 스키마 버전 필드 포함 → 향후 마이그레이션 대응

### 4.2 씬 데이터 구조 (초안)

```json
{
  "schemaVersion": "1.0",
  "scene": {
    "id": "uuid",
    "name": "업무",
    "icon": "work.png",
    "hotkey": "Ctrl+Alt+1",
    "closePreviousScene": true,
    "programs": [
      {
        "id": "uuid",
        "name": "Chrome",
        "execPath": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
        "arguments": "--profile-directory=\"Profile 1\"",
        "order": 1,
        "delayAfterMs": 2000,
        "dependsOn": null,
        "closeOnSceneExit": true,
        "window": {
          "monitor": 0,
          "x": 0,
          "y": 0,
          "width": 1920,
          "height": 1080,
          "state": "maximized"
        }
      },
      {
        "id": "uuid",
        "name": "Slack",
        "execPath": "C:\\Users\\user\\AppData\\Local\\slack\\slack.exe",
        "arguments": "",
        "order": 2,
        "delayAfterMs": 1000,
        "dependsOn": null,
        "closeOnSceneExit": true,
        "window": {
          "monitor": 1,
          "x": 0,
          "y": 0,
          "width": 960,
          "height": 1080,
          "state": "normal"
        }
      }
    ],
    "audio": {
      "masterVolume": 80,
      "defaultOutputDevice": "Speakers (Realtek)",
      "defaultInputDevice": "Microphone (USB)",
      "inputVolume": 75,
      "appVolumes": [
        {
          "processName": "chrome.exe",
          "volume": 60
        },
        {
          "processName": "slack.exe",
          "volume": 40
        }
      ]
    },
    "monitorConfig": {
      "monitors": [
        {
          "index": 0,
          "resolution": "1920x1080",
          "dpi": 100,
          "position": { "x": 0, "y": 0 }
        },
        {
          "index": 1,
          "resolution": "1920x1080",
          "dpi": 100,
          "position": { "x": 1920, "y": 0 }
        }
      ]
    }
  }
}
```

### 4.3 설정 파일 구조 (안)

```
SceneManager/
├── config.json              # 글로벌 설정 (자동실행, 위젯 위치, 단축키 등)
├── scenes/
│   ├── work.scene.json
│   ├── gaming.scene.json
│   └── netflix.scene.json
├── filters/
│   └── process-filter.json  # 블랙리스트/화이트리스트
└── logs/
    └── 2026-07-12.log
```

---

## 5. 기술 스택 (권장)

> 오픈소스 배포를 전제로, 커뮤니티 접근성과 크로스 빌드 편의성 고려

### 5.1 권장안: C# + .NET 8 + WPF

| 영역 | 기술 | 이유 |
|------|------|------|
| 언어 | C# | 기존 역량 활용, Windows API 친화적, 오픈소스 커뮤니티 크기 |
| 런타임 | .NET 8 (LTS) | MIT 라이선스, 장기 지원, 최신 API |
| UI | WPF | 바탕화면 위젯, 투명도, 애니메이션에 WinForm보다 적합 |
| Win API | P/Invoke (user32.dll, kernel32.dll) | SetWindowPos, FindWindow, EnumWindows 등 |
| 사운드 | NAudio (MIT) | Core Audio API 래핑, 앱별 볼륨/장치 제어 |
| 설정 | System.Text.Json | 내장 JSON 직렬화, 외부 의존성 없음 |
| 로깅 | Serilog (Apache 2.0) | 파일/콘솔 로깅, 구조화된 로그 |
| 빌드/배포 | GitHub Actions + MSBuild | CI/CD 자동화, Releases 배포 |
| 설치 | Inno Setup 또는 NSIS | 무료 Installer + Portable ZIP 동시 제공 |


### 5.3 핵심 Win32 API 목록

```
[프로세스/윈도우]
- Process.Start()                    // 프로세스 실행
- EnumWindows()                      // 활성 윈도우 열거
- FindWindow() / FindWindowEx()      // 특정 윈도우 찾기
- GetWindowRect() / SetWindowPos()   // 윈도우 위치/크기 제어
- ShowWindow()                       // 최대화/최소화/복원
- GetWindowThreadProcessId()         // 윈도우 → 프로세스 매핑

[핫키]
- RegisterHotKey()                   // 글로벌 단축키 등록
- UnregisterHotKey()                 // 단축키 해제

[모니터]
- EnumDisplayMonitors()              // 모니터 열거
- GetMonitorInfo()                   // 모니터 정보
- GetDpiForMonitor()                 // DPI 정보

[사운드 - Core Audio API via NAudio]
- MMDeviceEnumerator                 // 오디오 장치 열거
- AudioEndpointVolume               // 마스터 볼륨
- AudioSessionManager               // 앱별 세션 제어
```

---

## 6. 개발 단계 (Phase)

### Phase 1 — MVP (1차 버전)

핵심 기능만 포함. 실사용 가능한 최소 단위.

- [x] 씬 생성/편집/삭제/복제
- [x] 스냅샷 캡처 (전체 + 부분)
- [x] 프로세스 필터링 (블랙리스트 내장 + 사용자 편집)
- [x] 스냅샷 편집 UI
- [x] 프로그램 실행 (순차, 딜레이, 의존성)
- [x] 화면 배치 복원 (멀티모니터 대응)
- [x] 사운드 제어 (앱별 볼륨 + 출력/입력 장치 전환)
- [x] 씬 전환 시 이전 프로그램 종료 (기본값 ON, 옵션)
- [x] 글로벌 핫키 (씬별 단축키, 충돌 감지)
- [x] 바탕화면 위젯 UI
- [x] 시스템 트레이 상주
- [x] Windows 시작 시 자동실행 옵션
- [x] JSON 기반 설정 저장/로드
- [x] 씬 Import/Export
- [x] 로그 시스템
- [x] 실행 경로 유효성 검증
- [x] 프로그램별 종료 여부 플래그
- [x] 관리자 권한 프로세스 대응

### Phase 2 — 안정화 + 편의기능

- [ ] 실행 실패 시 3회 재시도
- [ ] 중복 실행 방지
- [ ] 프로그램 자동 로그인
- [ ] 시간대별 자동 씬 전환 (스케줄러)
- [ ] DPI 스케일링 보정 고도화
- [ ] 설정 스키마 마이그레이션 (버전 업 대응)

### Phase 3 — 오픈소스 생태계

- [ ] 플러그인/확장 인터페이스
- [ ] 다국어(i18n) 지원
- [ ] 커뮤니티 씬 프리셋 공유 (GitHub 기반 or 별도 허브)
- [ ] 자동 업데이트 (GitHub Releases 기반)
- [ ] UWP/Store 앱 대응