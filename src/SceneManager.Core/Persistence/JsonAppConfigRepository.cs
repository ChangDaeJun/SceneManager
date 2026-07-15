using System.Text.Encodings.Web;
using System.Text.Json;
using SceneManager.Core.Models;

namespace SceneManager.Core.Persistence;

/// <summary>
/// 앱 설정(<see cref="AppConfig"/>)을 단일 JSON 파일(app-config.json)로 영속화한다.
///
/// <para>로드 규칙(버전 병합):</para>
/// <list type="bullet">
///   <item>파일이 없거나 손상됨 → 내장 기본값으로 새로 만들어 저장(구 필터 파일이 있으면 사용자 목록 이관).</item>
///   <item>파일의 <see cref="AppConfig.Version"/>이 <see cref="AppConfig.CurrentVersion"/>보다 낮음
///         → 관리 섹션(시스템 블랙리스트·인자 설정)을 기본값으로 갱신하고 사용자 블랙/화이트리스트는 보존해 다시 저장.</item>
///   <item>버전이 같으면 파일 내용을 그대로 사용(사용자가 직접 편집한 값 존중).</item>
/// </list>
/// </summary>
public sealed class JsonAppConfigRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        // 한글·기호가 \uXXXX로 이스케이프되지 않고 그대로 보여야 사용자가 파일을 편집하기 쉽다.
        // 로컬 설정 파일이라 HTML 삽입 위험이 없어 relaxed 인코더가 안전하다.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly string _filePath;
    private readonly string? _legacyFilterPath;

    /// <param name="filePath">app-config.json 경로.</param>
    /// <param name="legacyFilterPath">구 버전 process-filter.json 경로(있으면 사용자 목록을 이관). 없으면 null.</param>
    public JsonAppConfigRepository(string filePath, string? legacyFilterPath = null)
    {
        _filePath = filePath;
        _legacyFilterPath = legacyFilterPath;
    }

    public AppConfig LoadOrCreate()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
                if (loaded is not null)
                {
                    if (loaded.Version < AppConfig.CurrentVersion)
                    {
                        var merged = MergeIntoDefaults(loaded);
                        Save(merged);
                        return merged;
                    }
                    return loaded;
                }
            }
            catch (JsonException)
            {
                // 손상된 파일 → 기본값으로 대체
            }
        }

        var created = AppConfig.CreateDefault();
        MigrateLegacyUserLists(created);
        Save(created);
        return created;
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(config, JsonOptions));
    }

    /// <summary>관리 섹션은 새 기본값으로, 사용자 소유 목록은 기존 값으로 보존해 합친다.</summary>
    private static AppConfig MergeIntoDefaults(AppConfig old)
    {
        var fresh = AppConfig.CreateDefault();
        fresh.Filter.UserBlacklist = old.Filter?.UserBlacklist ?? [];
        fresh.Filter.UserWhitelist = old.Filter?.UserWhitelist ?? [];
        return fresh;
    }

    /// <summary>구 process-filter.json이 있으면 사용자 블랙/화이트리스트만 새 설정으로 옮긴다.</summary>
    private void MigrateLegacyUserLists(AppConfig target)
    {
        if (string.IsNullOrEmpty(_legacyFilterPath) || !File.Exists(_legacyFilterPath))
            return;

        try
        {
            var json = File.ReadAllText(_legacyFilterPath);
            var legacy = JsonSerializer.Deserialize<ProcessFilter>(json, JsonOptions);
            if (legacy is not null)
            {
                target.Filter.UserBlacklist = legacy.UserBlacklist;
                target.Filter.UserWhitelist = legacy.UserWhitelist;
            }
        }
        catch (JsonException)
        {
            // 구 파일 손상 → 무시
        }
    }
}
