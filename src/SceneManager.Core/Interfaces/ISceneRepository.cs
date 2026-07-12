using SceneManager.Core.Models;

namespace SceneManager.Core.Interfaces;

/// <summary>
/// 씬의 영속화(저장/로드)를 담당한다. 구현체는 JSON 파일 기반(JsonSceneRepository)이지만,
/// 인터페이스는 저장 방식에 의존하지 않는다.
/// </summary>
public interface ISceneRepository
{
    /// <summary>저장된 모든 씬을 불러온다.</summary>
    Task<List<Scene>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>ID로 씬을 찾는다. 없으면 null.</summary>
    Task<Scene?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>이름으로 씬을 찾는다. 없으면 null.</summary>
    Task<Scene?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>씬을 저장한다(신규 생성 또는 덮어쓰기).</summary>
    Task SaveAsync(Scene scene, CancellationToken cancellationToken = default);

    /// <summary>ID로 씬을 삭제한다.</summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>외부 JSON 파일에서 씬을 가져온다.</summary>
    Task<Scene> ImportAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>씬을 외부 JSON 파일로 내보낸다.</summary>
    Task ExportAsync(string id, string filePath, CancellationToken cancellationToken = default);
}
