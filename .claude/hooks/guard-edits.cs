// PreToolUse hook for Edit, Write, MultiEdit and NotebookEdit.
// Blocks edits Claude must never make: generated code, DbUp scripts that git already
// tracks (someone may have applied them), and secrets. Exit code 2 blocks the edit
// and sends the reason on stderr back to Claude.

using System.Diagnostics;
using System.Text.Json;

var input = JsonDocument.Parse(Console.In.ReadToEnd()).RootElement;
if (!input.TryGetProperty("tool_input", out var toolInput))
{
    return 0;
}

var filePath = toolInput.TryGetProperty("file_path", out var p) ? p.GetString()
    : toolInput.TryGetProperty("notebook_path", out var n) ? n.GetString()
    : null;
if (string.IsNullOrEmpty(filePath))
{
    return 0;
}

var projectDir = Environment.GetEnvironmentVariable("CLAUDE_PROJECT_DIR")
    ?? (input.TryGetProperty("cwd", out var cwd) ? cwd.GetString() : null)
    ?? Directory.GetCurrentDirectory();

var relative = Path.GetRelativePath(projectDir, Path.GetFullPath(filePath, projectDir)).Replace('\\', '/');
var segments = relative.Split('/');
var fileName = segments[^1];

string? reason = null;

if (segments.Contains("bin") || segments.Contains("obj"))
{
    reason = "build output (bin/ or obj/) is generated. Change the source instead.";
}
else if (fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
    || fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
    || relative.Contains("/Internal/Generated/", StringComparison.OrdinalIgnoreCase))
{
    reason = "this file is generated (source generators or Marten/Wolverine code generation). Change what generates it.";
}
else if (fileName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
    && segments.Contains("Scripts")
    && IsTrackedByGit(projectDir, relative))
{
    reason = "this DbUp script is already in git, so a database may have run it. "
        + "DbUp never re-runs a script; add a new numbered script instead.";
}
else if (IsSecret(fileName))
{
    reason = "this file holds secrets. Use `dotnet user-secrets` locally; never write secrets to the repo.";
}

if (reason is null)
{
    return 0;
}

Console.Error.WriteLine($"Blocked edit to {relative}: {reason}");
return 2;

static bool IsSecret(string fileName) =>
    fileName.Equals(".env", StringComparison.OrdinalIgnoreCase)
    || fileName.StartsWith(".env.", StringComparison.OrdinalIgnoreCase)
    || fileName.Equals("secrets.json", StringComparison.OrdinalIgnoreCase)
    || new[] { ".pfx", ".p12", ".key", ".pem" }.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

static bool IsTrackedByGit(string projectDir, string relativePath)
{
    using var git = Process.Start(new ProcessStartInfo("git", ["ls-files", "--error-unmatch", "--", relativePath])
    {
        WorkingDirectory = projectDir,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    })!;
    git.WaitForExit();
    return git.ExitCode == 0;
}
