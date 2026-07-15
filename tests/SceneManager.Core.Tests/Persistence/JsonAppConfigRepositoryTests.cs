using System.Text.Json;
using SceneManager.Core.Models;
using SceneManager.Core.Persistence;

namespace SceneManager.Core.Tests.Persistence;

public class JsonAppConfigRepositoryTests : IDisposable
{
    private readonly string _dir;
    private readonly string _configPath;

    public JsonAppConfigRepositoryTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "sm-cfg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _configPath = Path.Combine(_dir, "app-config.json");
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* 정리 실패 무시 */ }
    }

    [Fact]
    public void LoadOrCreate_MissingFile_WritesDefaults()
    {
        var repo = new JsonAppConfigRepository(_configPath);

        var config = repo.LoadOrCreate();

        Assert.True(File.Exists(_configPath));
        Assert.Equal(AppConfig.CurrentVersion, config.Version);
        Assert.Contains("chrome", config.Arguments.Browsers);
        Assert.False(config.Filter.ShouldInclude("TextInputHost"));
    }

    [Fact]
    public void LoadOrCreate_SameVersion_KeepsUserEditsToManagedSections()
    {
        // 사용자가 관리 섹션(브라우저 목록)을 직접 편집. 버전은 현재와 동일.
        var edited = AppConfig.CreateDefault();
        edited.Arguments.Browsers = ["mycustombrowser"];
        Write(edited);

        var config = new JsonAppConfigRepository(_configPath).LoadOrCreate();

        Assert.Equal(new[] { "mycustombrowser" }, config.Arguments.Browsers);
    }

    [Fact]
    public void LoadOrCreate_OlderVersion_RefreshesManagedButKeepsUserLists()
    {
        // 구 버전 파일: 관리 섹션은 낡았고, 사용자 목록은 채워져 있음.
        var old = AppConfig.CreateDefault();
        old.Version = AppConfig.CurrentVersion - 1;
        old.Arguments.Browsers = ["staleonly"];              // 관리 섹션(갱신 대상)
        old.Filter.UserBlacklist = ["MyGame"];               // 사용자 소유(보존 대상)
        old.Filter.UserWhitelist = ["explorer"];
        Write(old);

        var config = new JsonAppConfigRepository(_configPath).LoadOrCreate();

        Assert.Equal(AppConfig.CurrentVersion, config.Version);       // 버전 갱신
        Assert.Contains("chrome", config.Arguments.Browsers);        // 관리 섹션 기본값으로 갱신
        Assert.DoesNotContain("staleonly", config.Arguments.Browsers);
        Assert.Contains("MyGame", config.Filter.UserBlacklist);      // 사용자 목록 보존
        Assert.Contains("explorer", config.Filter.UserWhitelist);
    }

    [Fact]
    public void LoadOrCreate_MigratesUserLists_FromLegacyFilterFile()
    {
        var legacyPath = Path.Combine(_dir, "process-filter.json");
        var legacy = new ProcessFilter { UserBlacklist = ["Discord"], UserWhitelist = ["explorer"] };
        File.WriteAllText(legacyPath, JsonSerializer.Serialize(legacy,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

        var config = new JsonAppConfigRepository(_configPath, legacyPath).LoadOrCreate();

        Assert.Contains("Discord", config.Filter.UserBlacklist);
        Assert.Contains("explorer", config.Filter.UserWhitelist);
    }

    private void Write(AppConfig config) => File.WriteAllText(_configPath,
        JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }));
}
