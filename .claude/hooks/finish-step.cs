// Stop hook. Before Claude reports that it is done, two checks run in order. Exit code 2
// keeps Claude working and sends the reason back as its next instruction.
//
// 1. Tests: run the tests affected by the changes on this branch.
//    - Affected: a module's own test project, plus the architecture tests for any source
//      change. Shared code (SharedKernel, Api, ServiceDefaults, build files) runs everything.
//    - The last passing state is cached in .claude/.cache, so stopping twice without new
//      changes doesn't rerun the tests.
// 2. Commit: if the tests pass and work is still uncommitted, ask Claude to commit the
//    finished step with a descriptive message (never on master, never a push).
//
// stop_hook_active is true when Claude is already continuing because of this hook. Then
// the hook lets Claude stop, so a test it can't fix, or a step it has deliberately left
// open to ask the author something, never becomes an endless loop.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var input = JsonDocument.Parse(Console.In.ReadToEnd()).RootElement;
if (input.TryGetProperty("stop_hook_active", out var active) && active.GetBoolean())
{
    return 0;
}

var projectDir = Environment.GetEnvironmentVariable("CLAUDE_PROJECT_DIR")
    ?? (input.TryGetProperty("cwd", out var cwd) ? cwd.GetString() : null)
    ?? Directory.GetCurrentDirectory();

if (RunAffectedTests(projectDir) is { } failures)
{
    Console.Error.WriteLine("Tests affected by your changes are failing. Fix them before finishing:");
    Console.Error.WriteLine(failures);
    return 2;
}

var uncommitted = Git(projectDir, "status", "--porcelain")
    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
if (uncommitted.Length == 0)
{
    return 0;
}

var branch = Git(projectDir, "branch", "--show-current").Trim();
if (branch is "master" or "main")
{
    Console.Error.WriteLine(
        $"There are {uncommitted.Length} uncommitted changes on {branch}. Work happens on an article branch: "
        + "create one (for example `git switch -c article-NN`) and commit there, or tell the author why not.");
    return 2;
}

Console.Error.WriteLine(
    $"The tests pass and {uncommitted.Length} changes on {branch} are uncommitted:\n  "
    + string.Join("\n  ", uncommitted.Take(15))
    + (uncommitted.Length > 15 ? "\n  ..." : "")
    + "\nIf this step is finished, commit it now: stage the files that belong to it and write a "
    + "conventional commit message (`feat:`, `test:`, `docs:`, `chore:`) that says what the step does. "
    + "Don't push. If the step is deliberately unfinished because you are asking the author something, "
    + "say so and stop.");
return 2;

// Returns the failure output, or null when nothing failed or nothing needed testing.
static string? RunAffectedTests(string projectDir)
{
    if (!File.Exists(Path.Combine(projectDir, "Staybook.slnx")))
    {
        return null;
    }

    // Changes on this branch: committed since it left master, staged, unstaged and untracked.
    var mergeBase = Git(projectDir, "merge-base", "HEAD", "master").Trim();
    var changed = new HashSet<string>(StringComparer.Ordinal);
    if (mergeBase.Length > 0)
    {
        AddLines(changed, Git(projectDir, "diff", "--name-only", mergeBase));
    }

    AddLines(changed, Git(projectDir, "diff", "--name-only", "HEAD"));
    AddLines(changed, Git(projectDir, "ls-files", "--others", "--exclude-standard"));

    var codeChanges = changed.Where(IsCode).ToList();
    if (codeChanges.Count == 0)
    {
        return null;
    }

    // The fingerprint covers file contents, not HEAD, so committing green work doesn't
    // trigger another test run.
    var cacheFile = Path.Combine(projectDir, ".claude", ".cache", "last-green");
    var state = Fingerprint(projectDir, codeChanges);
    if (File.Exists(cacheFile) && File.ReadAllText(cacheFile) == state)
    {
        return null;
    }

    var failures = new StringBuilder();
    foreach (var testProject in AffectedTestProjects(projectDir, codeChanges))
    {
        var (exitCode, output) = Run(projectDir, "dotnet", "test", "--project", testProject);
        if (exitCode != 0)
        {
            failures.AppendLine($"--- {testProject} ---");
            failures.AppendLine(Tail(output, 60));
        }
    }

    if (failures.Length > 0)
    {
        return failures.ToString();
    }

    Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
    File.WriteAllText(cacheFile, state);
    return null;
}

static bool IsCode(string path) =>
    (path.StartsWith("src/", StringComparison.Ordinal) || path.StartsWith("tests/", StringComparison.Ordinal)
        || !path.Contains('/'))
    && Path.GetExtension(path) is ".cs" or ".csproj" or ".props" or ".targets" or ".slnx" or ".json";

static List<string> AffectedTestProjects(string projectDir, List<string> changes)
{
    var all = Directory.Exists(Path.Combine(projectDir, "tests"))
        ? Directory.GetFiles(Path.Combine(projectDir, "tests"), "*.csproj", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(projectDir, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToList()
        : [];

    var runEverything = changes.Any(c => !c.StartsWith("src/Modules/", StringComparison.Ordinal)
        && !c.StartsWith("tests/", StringComparison.Ordinal));
    if (runEverything)
    {
        return all;
    }

    var affected = new SortedSet<string>(StringComparer.Ordinal);
    foreach (var change in changes)
    {
        var parts = change.Split('/');
        if (parts is ["src", "Modules", var module, ..])
        {
            affected.UnionWith(all.Where(t => t.StartsWith($"tests/Modules/{module}/", StringComparison.Ordinal)));
            affected.UnionWith(all.Where(t => t.StartsWith("tests/Staybook.ArchitectureTests/", StringComparison.Ordinal)));
        }
        else if (parts is ["tests", ..])
        {
            affected.UnionWith(all.Where(t => change.StartsWith(Path.GetDirectoryName(t)!.Replace('\\', '/') + "/", StringComparison.Ordinal)));
        }
    }

    return [.. affected];
}

static string Fingerprint(string projectDir, List<string> changes)
{
    var builder = new StringBuilder();
    foreach (var change in changes.Order(StringComparer.Ordinal))
    {
        var full = Path.Combine(projectDir, change);
        builder.Append(change).Append(':').Append(File.Exists(full) ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(full))) : "deleted");
    }

    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
}

static void AddLines(HashSet<string> set, string text)
{
    foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        set.Add(line);
    }
}

static string Tail(string text, int lines)
{
    var all = text.Split('\n');
    return string.Join('\n', all.Skip(Math.Max(0, all.Length - lines)));
}

static string Git(string workingDirectory, params string[] arguments) => Run(workingDirectory, "git", arguments).Output;

static (int ExitCode, string Output) Run(string workingDirectory, string fileName, params string[] arguments)
{
    using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
    {
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    })!;
    var stdout = process.StandardOutput.ReadToEndAsync();
    var stderr = process.StandardError.ReadToEndAsync();
    process.WaitForExit();
    return (process.ExitCode, stdout.Result + stderr.Result);
}
