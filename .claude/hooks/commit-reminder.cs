// PostToolUse hook for Edit, Write and MultiEdit.
// During long work, uncommitted changes pile up. Once they pass a size threshold, add a
// note to Claude's context (additionalContext) asking it to commit at the next point where
// the step builds and its tests pass. It never commits by itself and never blocks.
//
// The note is sent at most once per commit: after it fires, it stays quiet until HEAD moves.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

const int FileThreshold = 12;
const int LineThreshold = 400;

var input = JsonDocument.Parse(Console.In.ReadToEnd()).RootElement;
var projectDir = Environment.GetEnvironmentVariable("CLAUDE_PROJECT_DIR")
    ?? (input.TryGetProperty("cwd", out var cwd) ? cwd.GetString() : null)
    ?? Directory.GetCurrentDirectory();

var head = Git(projectDir, "rev-parse", "HEAD").Trim();
var cacheFile = Path.Combine(projectDir, ".claude", ".cache", "commit-reminded");
if (head.Length == 0 || (File.Exists(cacheFile) && File.ReadAllText(cacheFile) == head))
{
    return 0;
}

var files = Git(projectDir, "status", "--porcelain")
    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var changedLines = ChangedLines(Git(projectDir, "diff", "HEAD", "--numstat", "-M"))
    + UntrackedLines(projectDir, Git(projectDir, "ls-files", "--others", "--exclude-standard"));

if (files.Length < FileThreshold && changedLines < LineThreshold)
{
    return 0;
}

Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
File.WriteAllText(cacheFile, head);

var note = $"{files.Length} files and about {changedLines} lines are uncommitted since the last commit. "
    + "At the next point where this step builds and its tests pass, commit it with a descriptive "
    + "conventional commit message before starting anything new. Split unrelated work into separate commits. Don't push.";

// JsonObject instead of an anonymous type: file-based apps disable reflection-based
// serialization by default.
Console.WriteLine(new JsonObject
{
    ["hookSpecificOutput"] = new JsonObject
    {
        ["hookEventName"] = "PostToolUse",
        ["additionalContext"] = note,
    },
}.ToJsonString());
return 0;

// `git diff --numstat` prints "added<TAB>deleted<TAB>path"; binary files print "-".
static int ChangedLines(string numstat) =>
    numstat.Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(line => line.Split('\t'))
        .Where(parts => parts.Length >= 2)
        .Sum(parts => (int.TryParse(parts[0], out var added) ? added : 0) + (int.TryParse(parts[1], out var deleted) ? deleted : 0));

static int UntrackedLines(string projectDir, string untracked) =>
    untracked.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(path => Path.Combine(projectDir, path))
        .Where(path => File.Exists(path) && new FileInfo(path).Length < 1_000_000)
        .Sum(path => File.ReadLines(path).Count());

static string Git(string workingDirectory, params string[] arguments)
{
    using var process = Process.Start(new ProcessStartInfo("git", arguments)
    {
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    })!;
    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    return output;
}
