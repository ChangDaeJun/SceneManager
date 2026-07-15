<div align="center">
  <img src="assets/icon.svg" width="96" height="96" alt="SceneManager" />

# SceneManager

**실행 중인 프로그램과 창 배치를 "씬"으로 저장했다가, 바탕화면 바로가기 하나로 복원하는 Windows 유틸리티.**

[![license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6)

</div>

---

## 이게 뭔가요?

작업할 때마다 늘 같은 프로그램들을 같은 자리에 띄우나요? SceneManager는 지금 열려 있는 창들의
**프로그램 · 위치 · 크기 · 상태**를 통째로 "씬"으로 저장합니다. 저장한 씬은 바탕화면 아이콘이
되고, 더블클릭하면 그 프로그램들이 다시 실행되며 저장했던 자리로 배치됩니다.

- **상주 프로그램이 없습니다.** 편집기로 씬을 만들고 나면 백그라운드에 뜬 채로 남는 것이 없습니다.
  복원은 가벼운 실행기(`SceneRunner.exe`)가 바로가기에서 잠깐 실행될 뿐입니다.
- **모든 씬은 사람이 읽을 수 있는 JSON**입니다(`%LOCALAPPDATA%\SceneManager\scenes`).

## 주요 기능

- 📸 **스냅샷** — 현재 창 배치를 한 번에 캡처.
- 🪄 **3단계 마법사** — 스냅샷 직후(또는 편집 시) 바로 다듬기:
  1. **미세조정** — 배치도와 표에서 x/y/w/h·상태를 편집하면 실제 창이 즉시 움직입니다.
     방향키 이동·`Ctrl`+방향키 크기·`Delete` 삭제·`Ctrl+Z` 실행취소·틈 메우기 제공.
  2. **실행 인자** — 파일/URL이 열린 상태로 복원되도록 프로그램별 인자를 확인. 모를 땐
     **"인자 찾기 도우미"** 가 AI에게 물어볼 프롬프트(한/영)를 만들어 줍니다.
  3. **우선순위·의존성** — 실행 순서와 선행 관계를 지정(순환 자동 검증).
- 🔗 **바탕화면 바로가기 생성** — 아이콘 더블클릭으로 씬 복원.
- 🧹 **실행 시 기존 창 모두 닫기**(`--clean`) · **모서리 각지게**(Windows 11 둥근 모서리 제거) 옵션.
- 🖥️ **다중 모니터 · Per-Monitor DPI** 인식, DWM 프레임 보정으로 좌표가 눈에 보이는 창과 일치.

## 설치

[**Releases**](https://github.com/ChangDaeJun/SceneManager/releases)에서 최신 버전을 받으세요.

| 파일 | 설명 |
|---|---|
| `SceneManager-setup-x.y.z.exe` | **설치 관리자**(권장). 관리자 권한 없이 사용자 폴더에 설치되고 시작 메뉴에 등록됩니다. |
| `SceneManager-portable-x.y.z-win-x64.zip` | **포터블**. 압축을 풀고 `SceneManager.exe` 실행. |

- **.NET 설치가 필요 없습니다** — 런타임이 포함되어 있습니다(self-contained, win-x64).
- ⚠️ **SmartScreen 경고**: 코드 서명을 하지 않은 오픈소스라 처음 실행 시 "Windows의 PC 보호"
  경고가 뜰 수 있습니다. **추가 정보 → 실행**을 누르면 실행됩니다.
- 📌 **포터블은 폴더를 옮기지 마세요.** 씬 바로가기는 실행기의 경로를 기억하므로, 폴더를 옮기면
  기존 바로가기가 동작하지 않습니다(설치 관리자는 고정 경로라 안전).

## 사용법

1. 원하는 프로그램들을 원하는 자리에 배치한 뒤 SceneManager에서 **📸 스냅샷**.
2. 이름을 정하고 **3단계 마법사**로 배치·인자·순서를 다듬습니다.
3. 씬을 선택하고 **🔗 바로가기 생성** → 바탕화면 아이콘 완성.
4. 이후 그 아이콘을 더블클릭하면 씬이 복원됩니다. 기존 씬을 고치려면 **✏ 편집**(같은 마법사 재사용).

## 데이터 · 설정 위치

모든 데이터는 설치 폴더 밖의 `%LOCALAPPDATA%\SceneManager\`에 있어 설치/제거/업데이트와 무관하게 보존됩니다.

- `scenes\*.json` — 저장된 씬(씬 하나당 파일 하나).
- `app-config.json` — **통합 설정**. 캡처에서 제외할 프로세스 필터, 인자 도우미의 대표 프로그램
  목록·힌트·프롬프트가 들어 있습니다. **직접 편집 가능**하며, 앱 버전이 올라가면 관리 항목만
  자동 갱신되고 사용자가 추가한 목록은 보존됩니다.
- `logs\runner-YYYYMMDD.log` — 실행기 로그.

AI로 씬 JSON을 자연어 편집하는 프롬프트는 [`docs/AI_Scene_Editor_Prompt.md`](docs/AI_Scene_Editor_Prompt.md) 참고.

## 소스에서 빌드

- 요구: **.NET 10 SDK**(대상 프레임워크 `net8.0-windows`), Windows 10/11.

```bash
git clone https://github.com/ChangDaeJun/SceneManager.git
cd SceneManager
dotnet build SceneManager.slnx -c Release
dotnet test                                   # 단위 테스트

# 배포 산출물(편집기 + 실행기를 한 폴더로, 자체 포함):
dotnet publish src/SceneManager -c Release -r win-x64 --self-contained true -o artifacts/publish
dotnet publish src/SceneRunner  -c Release -r win-x64 --self-contained true -o artifacts/publish
```

아키텍처 설명은 [`docs/SceneManager_Architecture_v1.md`](docs/SceneManager_Architecture_v1.md)에 있습니다.

## 라이선스

[MIT](LICENSE) © 2026 ChangDaeJun
