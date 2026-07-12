using SceneManager.Core.Models;
using SceneManager.Core.Services;

namespace SceneManager.Core.Tests.Services;

public class ProcessFilterEvaluatorTests
{
    [Fact]
    public void ShouldInclude_EmptyFilter_IncludesEverything()
    {
        var evaluator = new ProcessFilterEvaluator(new ProcessFilter());

        Assert.True(evaluator.ShouldInclude("notepad"));
        Assert.True(evaluator.ShouldInclude("chrome"));
    }

    [Fact]
    public void ShouldInclude_SystemBlacklisted_Excluded()
    {
        var evaluator = new ProcessFilterEvaluator(new ProcessFilter
        {
            SystemBlacklist = ["TextInputHost"],
        });

        Assert.False(evaluator.ShouldInclude("TextInputHost"));
        Assert.True(evaluator.ShouldInclude("notepad"));
    }

    [Fact]
    public void ShouldInclude_UserBlacklisted_Excluded()
    {
        var evaluator = new ProcessFilterEvaluator(new ProcessFilter
        {
            UserBlacklist = ["Discord"],
        });

        Assert.False(evaluator.ShouldInclude("Discord"));
    }

    [Fact]
    public void ShouldInclude_Whitelist_OverridesBlacklist()
    {
        var evaluator = new ProcessFilterEvaluator(new ProcessFilter
        {
            SystemBlacklist = ["explorer"],
            UserWhitelist = ["explorer"],
        });

        Assert.True(evaluator.ShouldInclude("explorer"));
    }

    [Fact]
    public void ShouldInclude_ComparisonIsCaseInsensitive()
    {
        var evaluator = new ProcessFilterEvaluator(new ProcessFilter
        {
            SystemBlacklist = ["TextInputHost"],
        });

        Assert.False(evaluator.ShouldInclude("textinputhost"));
        Assert.False(evaluator.ShouldInclude("TEXTINPUTHOST"));
    }

    [Fact]
    public void CreateSystemDefault_ExcludesKnownNoise_ButNotExplorer()
    {
        var evaluator = new ProcessFilterEvaluator(ProcessFilterEvaluator.CreateSystemDefault());

        Assert.False(evaluator.ShouldInclude("TextInputHost"));
        Assert.False(evaluator.ShouldInclude("ApplicationFrameHost"));
        Assert.False(evaluator.ShouldInclude("SystemSettings"));
        Assert.True(evaluator.ShouldInclude("explorer"));  // 파일 탐색기는 유효
        Assert.True(evaluator.ShouldInclude("notepad"));
    }
}
