using SceneManager.Core.Models;

namespace SceneManager.Core.Services;

/// <summary>
/// 프로그램 간 의존성(<see cref="ProgramEntry.DependsOnId"/>)을 위상 정렬해
/// 실행 순서를 결정한다. 순수 알고리즘이며 부작용이 없다.
/// </summary>
public sealed class DependencyResolver
{
    /// <summary>
    /// 프로그램들을 실행 그룹의 순차 목록으로 변환한다.
    /// 같은 그룹 안의 프로그램들은 서로 의존이 없어 병렬 실행이 가능하고,
    /// 그룹들은 앞에서 뒤로 순차 실행되어야 한다.
    /// 존재하지 않는 <see cref="ProgramEntry.DependsOnId"/>는 의존 없음으로 취급한다.
    /// </summary>
    /// <exception cref="CircularDependencyException">순환 의존성이 있을 때.</exception>
    public List<List<ProgramEntry>> Resolve(IReadOnlyList<ProgramEntry> programs)
    {
        var result = new List<List<ProgramEntry>>();
        if (programs.Count == 0)
            return result;

        // Id → 프로그램, Id → 원래 입력 순서(동률 정렬용)
        var nodes = new Dictionary<string, ProgramEntry>();
        var index = new Dictionary<string, int>();
        for (var i = 0; i < programs.Count; i++)
        {
            nodes[programs[i].Id] = programs[i];
            index[programs[i].Id] = i;
        }

        // 진입 차수(선행 개수)와 자식 목록(선행 → 후행)
        var inDegree = new Dictionary<string, int>();
        var children = new Dictionary<string, List<ProgramEntry>>();
        foreach (var p in programs)
            children[p.Id] = [];

        foreach (var p in programs)
        {
            if (p.DependsOnId is not null && nodes.ContainsKey(p.DependsOnId))
            {
                inDegree[p.Id] = 1;
                children[p.DependsOnId].Add(p);
            }
            else
            {
                // 의존 없음 또는 존재하지 않는 대상 → 선행 없음으로 취급
                inDegree[p.Id] = 0;
            }
        }

        // 같은 그룹 내부는 Order 오름차순, 동률이면 입력 순서로 정렬
        List<ProgramEntry> Sorted(IEnumerable<ProgramEntry> items) =>
            items.OrderBy(p => p.Order).ThenBy(p => index[p.Id]).ToList();

        var current = Sorted(programs.Where(p => inDegree[p.Id] == 0));
        var processed = 0;

        while (current.Count > 0)
        {
            result.Add(current);
            processed += current.Count;

            var next = new List<ProgramEntry>();
            foreach (var node in current)
            {
                foreach (var child in children[node.Id])
                {
                    inDegree[child.Id]--;
                    if (inDegree[child.Id] == 0)
                        next.Add(child);
                }
            }

            current = Sorted(next);
        }

        // 처리하지 못한 노드가 남았다면 순환이 존재하는 것
        if (processed < programs.Count)
            throw new CircularDependencyException(
                "프로그램 의존성에 순환이 존재해 실행 순서를 결정할 수 없습니다.");

        return result;
    }
}

/// <summary>
/// 프로그램 의존성 그래프에 순환이 존재할 때 발생한다.
/// </summary>
public sealed class CircularDependencyException : Exception
{
    public CircularDependencyException(string message) : base(message) { }
}
