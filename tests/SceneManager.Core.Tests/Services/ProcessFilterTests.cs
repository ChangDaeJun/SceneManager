using SceneManager.Core.Models;

namespace SceneManager.Core.Tests.Services;

public class ProcessFilterTests
{
    [Fact]
    public void ShouldInclude_EmptyFilter_IncludesEverything()
    {
        var filter = new ProcessFilter();

        Assert.True(filter.ShouldInclude("notepad"));
        Assert.True(filter.ShouldInclude("chrome"));
    }

    [Fact]
    public void ShouldInclude_SystemBlacklisted_Excluded()
    {
        var filter = new ProcessFilter { SystemBlacklist = ["TextInputHost"] };

        Assert.False(filter.ShouldInclude("TextInputHost"));
        Assert.True(filter.ShouldInclude("notepad"));
    }

    [Fact]
    public void ShouldInclude_UserBlacklisted_Excluded()
    {
        var filter = new ProcessFilter { UserBlacklist = ["Discord"] };

        Assert.False(filter.ShouldInclude("Discord"));
    }

    [Fact]
    public void ShouldInclude_Whitelist_OverridesBlacklist()
    {
        var filter = new ProcessFilter
        {
            SystemBlacklist = ["explorer"],
            UserWhitelist = ["explorer"],
        };

        Assert.True(filter.ShouldInclude("explorer"));
    }

    [Fact]
    public void ShouldInclude_ComparisonIsCaseInsensitive()
    {
        var filter = new ProcessFilter { SystemBlacklist = ["TextInputHost"] };

        Assert.False(filter.ShouldInclude("textinputhost"));
        Assert.False(filter.ShouldInclude("TEXTINPUTHOST"));
    }

    [Fact]
    public void CreateDefault_ExcludesKnownNoise_ButNotExplorer()
    {
        var filter = ProcessFilter.CreateDefault();

        Assert.False(filter.ShouldInclude("TextInputHost"));
        Assert.False(filter.ShouldInclude("ApplicationFrameHost"));
        Assert.False(filter.ShouldInclude("SystemSettings"));
        Assert.True(filter.ShouldInclude("explorer"));  // 파일 탐색기는 유효
        Assert.True(filter.ShouldInclude("notepad"));
    }
}
