namespace SceneManager.Core.Models;

/// <summary>
/// 앱 전역 설정. 프로세스 필터 + 인자 도우미(대표 프로그램 분류·힌트·프롬프트)를 한 파일
/// (app-config.json)에 담는다. 코드에 하드코딩하는 대신 JSON으로 관리해 업데이트를 쉽게 한다.
///
/// <para>
/// "관리 섹션"(<see cref="Filter"/>.SystemBlacklist, <see cref="Arguments"/> 전체)은 앱이 소유하며
/// <see cref="CurrentVersion"/>이 올라가면 자동 갱신된다. 사용자 소유인
/// <see cref="ProcessFilter.UserBlacklist"/>/<see cref="ProcessFilter.UserWhitelist"/>는 병합 시 보존된다.
/// 버전이 같은 동안에는 사용자가 파일을 직접 편집한 값(관리 섹션 포함)이 그대로 쓰인다.
/// </para>
/// </summary>
public sealed class AppConfig
{
    /// <summary>내장 기본값의 버전. 기본값을 바꿀 때 올린다(사용자 파일의 관리 섹션이 갱신됨).</summary>
    public const int CurrentVersion = 1;

    /// <summary>이 설정 파일이 기준으로 삼은 기본값 버전.</summary>
    public int Version { get; set; }

    /// <summary>스냅샷/정리에서 프로세스를 걸러내는 필터.</summary>
    public ProcessFilter Filter { get; set; } = new();

    /// <summary>인자 도우미(2단계) 설정: 대표 프로그램 분류·힌트·프롬프트.</summary>
    public ArgumentConfig Arguments { get; set; } = new();

    /// <summary>내장 기본값으로 새 설정을 만든다(파일이 없을 때 첫 생성용).</summary>
    public static AppConfig CreateDefault() => new()
    {
        Version = CurrentVersion,
        Filter = ProcessFilter.CreateDefault(),
        Arguments = ArgumentConfig.CreateDefault(),
    };
}

/// <summary>인자 도우미가 쓰는 대표 프로그램 분류·안내·프롬프트 묶음.</summary>
public sealed class ArgumentConfig
{
    /// <summary>파일을 인자로 받아 여는 문서형 앱(실행 파일 이름, 확장자 제외).</summary>
    public List<string> DocumentApps { get; set; } = [];

    /// <summary>URL을 인자로 받는 브라우저(실행 파일 이름).</summary>
    public List<string> Browsers { get; set; } = [];

    /// <summary>인자 입력 칸 위에 보여줄 상황별 안내 문구.</summary>
    public ArgumentHints Hints { get; set; } = new();

    /// <summary>"인자 찾기 도우미" AI 프롬프트 템플릿(한/영).</summary>
    public ArgumentPrompt Prompt { get; set; } = new();

    public static ArgumentConfig CreateDefault() => new()
    {
        DocumentApps =
        [
            "excel", "winword", "powerpnt", "onenote", "msaccess", "outlook",
            "hwp", "hword", "hcell", "hshow",                  // 한글/한컴오피스
            "notepad", "notepad++", "wordpad",
            "code", "devenv", "rider64", "pycharm64", "idea64", // 코드 에디터/IDE
            "acrord32", "acrobat", "sumatrapdf", "foxitreader", // PDF
            "photoshop", "illustrator", "gimp-2.10",            // 이미지
            "vlc", "wmplayer", "mpc-hc64", "potplayermini64",   // 미디어(파일 재생)
        ],
        Browsers =
        [
            "chrome", "msedge", "firefox", "whale", "opera", "brave",
            "iexplore", "vivaldi", "chromium",
        ],
        Hints = new ArgumentHints
        {
            BrowserWithArgs = "브라우저입니다. 인자가 설정되어 있습니다. 새 창으로 열려면 URL 앞에 --new-window 를 붙이세요.",
            BrowserNoArgs = "브라우저입니다. 열 URL을 인자로 넣으세요. 새 창: --new-window https://...  ·  여러 탭: URL을 공백으로 나열.",
            DocumentWithArgs = "문서형 앱입니다. 인자가 설정되어 있습니다. 파일 경로가 맞는지 확인하세요.",
            DocumentNoArgs = "문서형 앱입니다. 열 파일의 전체 경로를 따옴표로 감싸 넣으세요. 예: \"C:\\작업\\보고서.xlsx\"",
            GeneralWithArgs = "인자가 설정되어 있습니다. 특정 파일/URL을 열지 않는다면 비워도 됩니다.",
            GeneralNoArgs = "이 앱은 대개 인자 없이 실행됩니다. 특정 파일/URL을 열려면 아래에 인자를 입력하세요.",
        },
        Prompt = ArgumentPrompt.CreateDefault(),
    };
}

/// <summary>인자 입력 칸 위 안내(상황별). 6가지 경우.</summary>
public sealed class ArgumentHints
{
    public string BrowserWithArgs { get; set; } = "";
    public string BrowserNoArgs { get; set; } = "";
    public string DocumentWithArgs { get; set; } = "";
    public string DocumentNoArgs { get; set; } = "";
    public string GeneralWithArgs { get; set; } = "";
    public string GeneralNoArgs { get; set; } = "";
}

/// <summary>
/// "인자 찾기 도우미" 프롬프트 템플릿. 본문에 <c>{name} {kind} {execPath} {windowTitle} {browserNote}</c>
/// 자리표시자를 넣으면 <see cref="Services.ArgumentAdvisor"/>가 프로그램 정보로 치환한다.
/// </summary>
public sealed class ArgumentPrompt
{
    public string Korean { get; set; } = "";
    public string English { get; set; } = "";

    /// <summary>브라우저일 때만 {browserNote} 자리에 들어가는 문장(그 외에는 빈 문자열).</summary>
    public string BrowserNoteKorean { get; set; } = "";
    public string BrowserNoteEnglish { get; set; } = "";

    /// <summary>{kind} 자리에 들어가는 분류 라벨.</summary>
    public string KindBrowserKorean { get; set; } = "";
    public string KindDocumentKorean { get; set; } = "";
    public string KindGeneralKorean { get; set; } = "";
    public string KindBrowserEnglish { get; set; } = "";
    public string KindDocumentEnglish { get; set; } = "";
    public string KindGeneralEnglish { get; set; } = "";

    public static ArgumentPrompt CreateDefault() => new()
    {
        KindBrowserKorean = "브라우저",
        KindDocumentKorean = "문서형 앱",
        KindGeneralKorean = "일반 앱",
        KindBrowserEnglish = "web browser",
        KindDocumentEnglish = "document app",
        KindGeneralEnglish = "general app",
        BrowserNoteKorean =
            "\n   - 브라우저이므로: 새 창으로 열려면 `--new-window <URL>`, 여러 URL을 한 창의 탭들로 열려면 URL을 공백으로 나열.",
        BrowserNoteEnglish =
            "\n   - Since it is a browser: use `--new-window <URL>` to force a new window, or list URLs separated by spaces to open them as tabs in one window.",
        Korean =
            """
            나는 "SceneManager"라는 Windows 유틸리티로 프로그램 배치를 저장/복원하려고 해.
            아래 프로그램을 "특정 파일(또는 URL)이 열린 상태로" 자동 실행하려면, 실행 파일에 어떤
            명령줄 인자(command-line arguments)를 붙여야 하는지, 그리고 그 파일 경로나 URL을 내 PC에서
            어떻게 확인하는지 단계별로 알려줘.

            ## 프로그램 정보
            - 이름: {name}
            - 종류: {kind}
            - 실행 파일: {execPath}
            - 창 제목(참고): {windowTitle}

            ## 원하는 답
            1. 이 앱에 넘길 arguments 예시.
               - 파일이면 따옴표로 감싼 절대경로. 예: "C:\작업\보고서.xlsx"{browserNote}
            2. 그 파일의 실제 전체 경로(또는 URL)를 내 PC에서 확인하는 방법.
               (예: 최근 문서 목록, 창 제목 표시줄, 브라우저 주소창 복사, 파일 속성의 '위치' 등)
            3. 만약 이 앱이 파일/URL 인자를 받지 않는 종류라면 그렇다고 알려줘.

            마지막에 "SceneManager의 arguments 칸에 넣을 최종 문자열" 한 줄만 정리해줘.
            """,
        English =
            """
            I use a Windows utility called "SceneManager" to save and restore window layouts.
            For the program below, I want to auto-launch it "with a specific file (or URL) already open".
            Tell me, step by step, what command-line arguments I should pass to the executable, and how I
            can find that file's path or URL on my PC.

            ## Program
            - Name: {name}
            - Kind: {kind}
            - Executable: {execPath}
            - Window title (for reference): {windowTitle}

            ## What I want
            1. An example of the arguments to pass.
               - For a file, use the quoted absolute path, e.g. "C:\work\report.xlsx"{browserNote}
            2. How to find the actual full path (or URL) on my PC.
               (e.g. recent-files list, the window title bar, copying the browser address bar, file properties)
            3. If this app does not accept a file/URL argument, tell me so.

            At the end, give me a single line: "the final string to paste into SceneManager's arguments box".
            """,
    };
}
