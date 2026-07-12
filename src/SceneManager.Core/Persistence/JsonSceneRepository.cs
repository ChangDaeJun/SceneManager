using System.Text.Json;
using System.Text.Json.Serialization;
using SceneManager.Core.Interfaces;
using SceneManager.Core.Models;

namespace SceneManager.Core.Persistence;

/// <summary>
/// 씬을 JSON 파일로 영속화한다. 씬 하나당 파일 하나(<c>{이름}.json</c>).
/// </summary>
public sealed class JsonSceneRepository : ISceneRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _scenesDirectory;

    public JsonSceneRepository(string scenesDirectory) => _scenesDirectory = scenesDirectory;

    public async Task<List<Scene>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_scenesDirectory))
            return [];

        var scenes = new List<Scene>();
        foreach (var file in Directory.EnumerateFiles(_scenesDirectory, "*.json"))
        {
            var scene = await ReadSceneAsync(file, cancellationToken);
            if (scene is not null)
                scenes.Add(scene);
        }

        return scenes;
    }

    public async Task<Scene?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.FirstOrDefault(s => s.Id == id);
    }

    public async Task<Scene?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task SaveAsync(Scene scene, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_scenesDirectory);

        scene.Metadata.ModifiedAt = DateTimeOffset.Now;
        if (scene.Metadata.CreatedAt == default)
            scene.Metadata.CreatedAt = scene.Metadata.ModifiedAt;

        var path = GetFilePath(scene.Name);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, scene, JsonOptions, cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        var target = all.FirstOrDefault(s => s.Id == id);
        if (target is null)
            return;

        var path = GetFilePath(target.Name);
        if (File.Exists(path))
            File.Delete(path);
    }

    public Task<Scene> ImportAsync(string filePath, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("이후 단계에서 구현");

    public Task ExportAsync(string id, string filePath, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("이후 단계에서 구현");

    private async Task<Scene?> ReadSceneAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<Scene>(stream, JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return null; // 손상된 파일은 건너뛴다
        }
    }

    private string GetFilePath(string sceneName) => Path.Combine(_scenesDirectory, Sanitize(sceneName) + ".json");

    /// <summary>파일명으로 쓸 수 없는 문자를 <c>_</c>로 치환한다.</summary>
    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}
