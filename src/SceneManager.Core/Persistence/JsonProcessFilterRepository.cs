using System.Text.Json;
using SceneManager.Core.Models;

namespace SceneManager.Core.Persistence;

/// <summary>
/// 프로세스 필터를 JSON 파일(process-filter.json)로 영속화한다.
/// 씬과 무관한 전역 설정으로, 사용자가 파일을 직접 편집해 블랙/화이트리스트를 관리할 수 있다.
/// </summary>
public sealed class JsonProcessFilterRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _filePath;

    public JsonProcessFilterRepository(string filePath) => _filePath = filePath;

    /// <summary>
    /// 필터 파일을 로드한다. 파일이 없거나 손상됐으면 기본값(<see cref="ProcessFilter.CreateDefault"/>)을
    /// 만들어 파일로 저장한 뒤 반환한다(사용자가 편집할 수 있도록).
    /// </summary>
    public ProcessFilter LoadOrCreateDefault()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                var filter = JsonSerializer.Deserialize<ProcessFilter>(json, JsonOptions);
                if (filter is not null)
                    return filter;
            }
            catch (JsonException)
            {
                // 손상된 파일 → 기본값으로 대체
            }
        }

        var defaults = ProcessFilter.CreateDefault();
        Save(defaults);
        return defaults;
    }

    /// <summary>필터를 파일로 저장한다.</summary>
    public void Save(ProcessFilter filter)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(filter, JsonOptions));
    }
}
