using System.IO;
using SceneManager.Core.Models;

namespace SceneManager.Core.Services;

/// <summary>
/// 스냅샷 인자 단계에서 쓰는 도우미. 프로그램을 문서형/브라우저로 분류해 짧은 힌트를 주고,
/// "이 앱을 특정 파일·URL로 열려면 arguments에 무엇을 넣어야 하는지"를 다른 AI에게 물어볼
/// 프롬프트(한/영)를 만든다. 모든 목록·문구는 <see cref="ArgumentConfig"/>(app-config.json)에서 온다.
/// </summary>
public sealed class ArgumentAdvisor
{
    private const string WindowTitleEmptyKo = "(없음)";
    private const string WindowTitleEmptyEn = "(none)";

    private readonly ArgumentConfig _config;
    private readonly HashSet<string> _docApps;
    private readonly HashSet<string> _browsers;

    public ArgumentAdvisor(ArgumentConfig config)
    {
        _config = config;
        _docApps = new HashSet<string>(config.DocumentApps, StringComparer.OrdinalIgnoreCase);
        _browsers = new HashSet<string>(config.Browsers, StringComparer.OrdinalIgnoreCase);
    }

    private static string ExeName(ProgramEntry p)
        => string.IsNullOrWhiteSpace(p.ExecPath) ? p.Name : Path.GetFileNameWithoutExtension(p.ExecPath);

    public bool IsDocApp(ProgramEntry p) => _docApps.Contains(ExeName(p));
    public bool IsBrowser(ProgramEntry p) => _browsers.Contains(ExeName(p));

    /// <summary>인자 입력 칸 위에 보여줄 짧은 안내(항상 non-null).</summary>
    public string Hint(ProgramEntry p)
    {
        var hasArgs = !string.IsNullOrWhiteSpace(p.Arguments);
        var h = _config.Hints;

        if (IsBrowser(p))
            return hasArgs ? h.BrowserWithArgs : h.BrowserNoArgs;
        if (IsDocApp(p))
            return hasArgs ? h.DocumentWithArgs : h.DocumentNoArgs;
        return hasArgs ? h.GeneralWithArgs : h.GeneralNoArgs;
    }

    /// <summary>사용자가 다른 AI에게 붙여 물어볼 수 있는 한국어 프롬프트.</summary>
    public string BuildKorean(ProgramEntry p)
    {
        var pr = _config.Prompt;
        var kind = IsBrowser(p) ? pr.KindBrowserKorean : IsDocApp(p) ? pr.KindDocumentKorean : pr.KindGeneralKorean;
        var note = IsBrowser(p) ? pr.BrowserNoteKorean : "";
        return Fill(pr.Korean, p, kind, note, WindowTitleEmptyKo);
    }

    /// <summary>같은 내용의 영어 프롬프트.</summary>
    public string BuildEnglish(ProgramEntry p)
    {
        var pr = _config.Prompt;
        var kind = IsBrowser(p) ? pr.KindBrowserEnglish : IsDocApp(p) ? pr.KindDocumentEnglish : pr.KindGeneralEnglish;
        var note = IsBrowser(p) ? pr.BrowserNoteEnglish : "";
        return Fill(pr.English, p, kind, note, WindowTitleEmptyEn);
    }

    private static string Fill(string template, ProgramEntry p, string kind, string browserNote, string titleEmpty)
        => template
            .Replace("{name}", p.Name)
            .Replace("{kind}", kind)
            .Replace("{execPath}", p.ExecPath)
            .Replace("{windowTitle}", string.IsNullOrWhiteSpace(p.WindowTitle) ? titleEmpty : p.WindowTitle)
            .Replace("{browserNote}", browserNote);
}
