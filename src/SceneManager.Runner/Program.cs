// SceneManager.Runner — 씬 JSON을 실행/배치하는 무상태 실행기.
// 바탕화면 바로가기가 이 exe를 인자와 함께 실행한다.
//   SceneManager.Runner.exe <scene-name> [--clean]
//     <scene-name>  적용할 씬 이름 (AppData\SceneManager\scenes\{name}.json)
//     --clean       실행 전 현재 보이는 창을 모두 닫아 빈 바탕화면부터 시작

using SceneManager.Core.Models;
using SceneManager.Core.Persistence;
using SceneManager.Core.Platform;
using SceneManager.Core.Services;

var baseDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "SceneManager");
var scenesDir = Path.Combine(baseDir, "scenes");
var logsDir = Path.Combine(baseDir, "logs");
Directory.CreateDirectory(logsDir);
var logPath = Path.Combine(logsDir, $"runner-{DateTime.Now:yyyyMMdd}.log");

void Log(string message)
{
    var line = $"{DateTime.Now:HH:mm:ss.fff} {message}";
    Console.WriteLine(line);                 // 터미널 실행 시 보임
    File.AppendAllText(logPath, line + Environment.NewLine); // 더블클릭 실행 시 기록
}

// ── 인자 파싱 ──
var clean = args.Contains("--clean");
var sceneName = args.FirstOrDefault(a => !a.StartsWith("--"));

if (sceneName is null)
{
    Log("오류: 씬 이름이 없습니다. 사용법: SceneManager.Runner.exe <scene-name> [--clean]");
    return 2;
}

Log($"=== Runner 시작: scene='{sceneName}' clean={clean} ===");

var repository = new JsonSceneRepository(scenesDir);
var scene = await repository.GetByNameAsync(sceneName);
if (scene is null)
{
    Log($"오류: 씬 '{sceneName}'을(를) 찾을 수 없습니다. ({scenesDir})");
    return 2;
}

var filter = new JsonProcessFilterRepository(Path.Combine(baseDir, "process-filter.json"))
    .LoadOrCreateDefault();

var engine = new SceneRunner(
    repository,
    new WindowsDesktopManager(),
    filter);

engine.ProgressChanged += (_, e) => Log($"  [{e.CurrentStep}/{e.TotalSteps}] {e.StepDescription}");

if (clean)
{
    var closed = await engine.ClearAsync();
    Log($"화면 정리(--clean): {closed}개 창 닫기 요청");
    await Task.Delay(800); // 창이 실제로 닫힐 시간
}

var result = await engine.ApplyAsync(scene.Id);
Log($"적용 완료: {result.Elapsed.TotalSeconds:F1}s, 성공={result.Success}");
foreach (var step in result.Steps)
    Log($"  {(step.Success ? "OK " : "FAIL")} {step.StepName}{(step.ErrorMessage is null ? "" : $" — {step.ErrorMessage}")}");

return result.Success ? 0 : 1;
