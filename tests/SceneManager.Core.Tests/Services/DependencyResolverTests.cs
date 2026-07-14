using SceneManager.Core.Models;
using SceneManager.Core.Services;

namespace SceneManager.Core.Tests.Services;

public class DependencyResolverTests
{

    /// <summary>테스트용 ProgramEntry 생성 헬퍼.</summary>
    private static ProgramEntry Program(string id, int order = 0, string? dependsOn = null) => new()
    {
        Id = id,
        Name = id,
        ExecPath = $"C:\\{id}.exe",
        Order = order,
        DependsOnId = dependsOn
    };

    [Fact]
    public void Resolve_EmptyList_ReturnsEmpty()
    {
        var result = DependencyResolver.Resolve([]);

        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_SingleProgram_ReturnsOneGroup()
    {
        var result = DependencyResolver.Resolve([Program("A")]);

        var group = Assert.Single(result);
        Assert.Equal(["A"], group.Select(p => p.Id));
    }

    [Fact]
    public void Resolve_IndependentPrograms_AllInFirstGroup_OrderedByOrder()
    {
        // A(order 2), B(order 1) → 의존 없음 → 한 그룹, Order 오름차순 정렬
        var result = DependencyResolver.Resolve([Program("A", order: 2), Program("B", order: 1)]);

        var group = Assert.Single(result);
        Assert.Equal(["B", "A"], group.Select(p => p.Id));
    }

    [Fact]
    public void Resolve_LinearChain_ProducesSequentialGroups()
    {
        // C→B→A (일부러 뒤섞어 입력)
        var result = DependencyResolver.Resolve([
            Program("C", dependsOn: "B"),
            Program("B", dependsOn: "A"),
            Program("A")
        ]);

        Assert.Equal(3, result.Count);
        Assert.Equal(["A"], result[0].Select(p => p.Id));
        Assert.Equal(["B"], result[1].Select(p => p.Id));
        Assert.Equal(["C"], result[2].Select(p => p.Id));
    }

    [Fact]
    public void Resolve_MixedDependencies_GroupsByDepth()
    {
        // 아키텍처 9장 예시: VPN·Chrome 독립(order 1), App→VPN, Msg→App
        var result = DependencyResolver.Resolve([
            Program("VPN", order: 1),
            Program("Chrome", order: 1),
            Program("App", dependsOn: "VPN"),
            Program("Msg", dependsOn: "App")
        ]);

        Assert.Equal(3, result.Count);
        Assert.Equal(["VPN", "Chrome"], result[0].Select(p => p.Id));
        Assert.Equal(["App"], result[1].Select(p => p.Id));
        Assert.Equal(["Msg"], result[2].Select(p => p.Id));
    }

    [Fact]
    public void Resolve_CircularDependency_Throws()
    {
        // A→B→A
        Assert.Throws<CircularDependencyException>(() =>
            DependencyResolver.Resolve([Program("A", dependsOn: "B"), Program("B", dependsOn: "A")]));
    }

    [Fact]
    public void Resolve_MissingDependencyTarget_TreatedAsIndependent()
    {
        // 존재하지 않는 대상에 의존 → 의존 없음으로 취급
        var result = DependencyResolver.Resolve([Program("A", dependsOn: "GHOST")]);

        var group = Assert.Single(result);
        Assert.Equal(["A"], group.Select(p => p.Id));
    }
}
