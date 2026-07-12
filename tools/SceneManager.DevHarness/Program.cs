// SceneManager.DevHarness — 개발용 실험 하네스 (배포 대상 아님).
// 세로 슬라이스(스냅샷 → 실행)를 손으로 돌려보기 위한 드라이버.

using SceneManager.Core.Models;
using SceneManager.Core.Persistence;
using SceneManager.Core.Platform;
using SceneManager.Core.Services;

if (args.Length == 0)
{
    PrintUsage();
    return;
}

switch (args[0].ToLowerInvariant())
{
    case "hello":
        Console.WriteLine($"SceneManager DevHarness OK (schema {SceneMetadata.CurrentSchemaVersion})");
        break;

    case "list-windows":
        ListWindows();
        break;

    case "snapshot":
        if (args.Length < 2)
        {
            Console.WriteLine("사용법: snapshot <name>");
            break;
        }
        await SnapshotAsync(args[1]);
        break;

    case "list-scenes":
        await ListScenesAsync();
        break;

    case "apply":
        if (args.Length < 2)
        {
            Console.WriteLine("사용법: apply <name> [--clean]");
            break;
        }
        await ApplyAsync(args[1], clean: args.Contains("--clean"));
        break;

    case "clear":
        await ClearAsync();
        break;

    default:
        Console.WriteLine($"알 수 없는 명령: {args[0]}");
        PrintUsage();
        break;
}

static void ListWindows()
{
    var windowManager = new WindowsWindowManager();
    var windows = windowManager.GetAllVisibleWindows();

    Console.WriteLine($"보이는 창 {windows.Count}개:");
    foreach (var w in windows.OrderBy(w => w.ProcessName, StringComparer.OrdinalIgnoreCase))
    {
        var p = w.Placement;
        Console.WriteLine(
            $"  [{w.ProcessName,-18}] ({p.X,5},{p.Y,5}) {p.Width,5}x{p.Height,-5}  \"{Truncate(w.WindowTitle, 45)}\"");
    }
}

static async Task SnapshotAsync(string name)
{
    var snapshotService = new SnapshotService(
        new WindowsWindowManager(),
        new ProcessFilterEvaluator(ProcessFilterEvaluator.CreateSystemDefault()));

    var scene = await snapshotService.CaptureFullAsync(name);

    var repository = new JsonSceneRepository(ScenesDirectory());
    await repository.SaveAsync(scene);

    Console.WriteLine($"씬 '{scene.Name}' 저장 ({scene.Programs.Count}개 프로그램):");
    foreach (var prog in scene.Programs.OrderBy(p => p.Order))
    {
        var win = prog.Window;
        var place = win is null ? "" : $"({win.X},{win.Y}) {win.Width}x{win.Height}";
        Console.WriteLine($"  {prog.Order,2}. [{prog.Name,-18}] {place}");
        Console.WriteLine($"      {prog.ExecPath}");
    }
    Console.WriteLine($"→ {System.IO.Path.Combine(ScenesDirectory(), name + ".json")}");
}

static async Task ApplyAsync(string name, bool clean)
{
    var repository = new JsonSceneRepository(ScenesDirectory());
    var scene = await repository.GetByNameAsync(name);
    if (scene is null)
    {
        Console.WriteLine($"씬 '{name}'을(를) 찾을 수 없음.");
        return;
    }

    var engine = CreateEngine(repository);
    engine.ProgressChanged += (_, e) =>
        Console.WriteLine($"  [{e.CurrentStep}/{e.TotalSteps}] {e.StepDescription}");

    if (clean)
    {
        var cleared = await engine.ClearAsync();
        Console.WriteLine($"기존 창 정리(--clean): {cleared}개 닫기 요청");
        await Task.Delay(800); // 창이 실제로 닫힐 시간을 준다
    }

    Console.WriteLine($"씬 '{scene.Name}' 적용 시작...");
    var result = await engine.ApplyAsync(scene.Id);

    Console.WriteLine($"완료 ({result.Elapsed.TotalSeconds:F1}s, 성공={result.Success}):");
    foreach (var step in result.Steps)
    {
        var mark = step.Success ? "✓" : "✗";
        var err = step.ErrorMessage is null ? "" : $" — {step.ErrorMessage}";
        Console.WriteLine($"  {mark} {step.StepName}{err}");
    }
}

static async Task ClearAsync()
{
    var engine = CreateEngine(new JsonSceneRepository(ScenesDirectory()));
    var cleared = await engine.ClearAsync();
    Console.WriteLine($"닫기 요청: {cleared}개 창");
}

static SceneEngine CreateEngine(JsonSceneRepository repository) => new(
    repository,
    new WindowsProcessManager(),
    new WindowsWindowManager(),
    new DependencyResolver(),
    new ProcessFilterEvaluator(ProcessFilterEvaluator.CreateSystemDefault()));

static async Task ListScenesAsync()
{
    var repository = new JsonSceneRepository(ScenesDirectory());
    var scenes = await repository.GetAllAsync();

    Console.WriteLine($"저장된 씬 {scenes.Count}개 ({ScenesDirectory()}):");
    foreach (var s in scenes)
        Console.WriteLine($"  {s.Name}  ({s.Programs.Count}개 프로그램, 수정 {s.Metadata.ModifiedAt:yyyy-MM-dd HH:mm})");
}

static string ScenesDirectory() => System.IO.Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "SceneManager", "scenes");

static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

static void PrintUsage()
{
    Console.WriteLine("사용법: dev-harness <command> [args]");
    Console.WriteLine("  hello                 동작 확인");
    Console.WriteLine("  list-windows          현재 보이는 창 목록");
    Console.WriteLine("  snapshot <name>       현재 창들을 씬으로 저장");
    Console.WriteLine("  list-scenes           저장된 씬 목록");
    Console.WriteLine("  apply <name> [--clean]  저장된 씬을 실행/배치 (--clean: 기존 창 먼저 정리)");
    Console.WriteLine("  clear                 현재 보이는 사용자 창을 모두 닫음(빈 바탕화면)");
}
