using SceneManager.Core.Models;
using SceneManager.Core.Services;

namespace SceneManager.Core.Tests.Services;

public class ArgumentAdvisorTests
{
    private static readonly ArgumentAdvisor Advisor = new(ArgumentConfig.CreateDefault());

    private static ProgramEntry Program(string exec, string? args = null, string? title = null) => new()
    {
        Id = "id",
        Name = "테스트앱",
        ExecPath = exec,
        Arguments = args,
        WindowTitle = title,
    };

    [Fact]
    public void Classifies_Browser_And_DocApp_ByExeName()
    {
        Assert.True(Advisor.IsBrowser(Program(@"C:\Program Files\Google\Chrome\chrome.exe")));
        Assert.True(Advisor.IsDocApp(Program(@"C:\Program Files\Microsoft Office\EXCEL.EXE")));
        Assert.False(Advisor.IsBrowser(Program(@"C:\Spotify\Spotify.exe")));
        Assert.False(Advisor.IsDocApp(Program(@"C:\Spotify\Spotify.exe")));
    }

    [Fact]
    public void Hint_PicksMessage_ByKindAndArguments()
    {
        var cfg = ArgumentConfig.CreateDefault();

        Assert.Equal(cfg.Hints.BrowserNoArgs, Advisor.Hint(Program("chrome.exe")));
        Assert.Equal(cfg.Hints.DocumentNoArgs, Advisor.Hint(Program("excel.exe")));
        Assert.Equal(cfg.Hints.GeneralNoArgs, Advisor.Hint(Program("spotify.exe")));
        Assert.Equal(cfg.Hints.DocumentWithArgs, Advisor.Hint(Program("excel.exe", args: "\"C:\\a.xlsx\"")));
    }

    [Fact]
    public void BuildKorean_SubstitutesPlaceholders_AndBrowserNote()
    {
        var prompt = Advisor.BuildKorean(Program("chrome.exe", title: "예시 페이지"));

        Assert.Contains("테스트앱", prompt);
        Assert.Contains("chrome.exe", prompt);
        Assert.Contains("예시 페이지", prompt);
        Assert.Contains("브라우저", prompt);            // kind
        Assert.Contains("--new-window", prompt);        // browserNote 삽입됨
        Assert.DoesNotContain("{name}", prompt);        // 자리표시자 모두 치환
        Assert.DoesNotContain("{browserNote}", prompt);
    }

    [Fact]
    public void BuildKorean_NonBrowser_OmitsBrowserNote_AndFillsEmptyTitle()
    {
        var prompt = Advisor.BuildKorean(Program("spotify.exe"));

        Assert.Contains("(없음)", prompt);              // windowTitle 비어 있음
        Assert.DoesNotContain("--new-window", prompt);  // 브라우저 아님 → note 없음
        Assert.DoesNotContain("{windowTitle}", prompt);
    }
}
