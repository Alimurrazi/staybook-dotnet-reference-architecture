// PostToolUse hook for Edit, Write and MultiEdit.
// After Claude changes a C# or MSBuild file: fix whitespace in that file, then build the
// project that owns it. Analyzers run during the build with warnings as errors, so style
// and correctness problems surface here. Exit code 2 sends the errors back to Claude.

using System.Diagnostics;
using System.Text.Json;

var input = JsonDocument.Parse(Console.In.ReadToEnd()).RootElement;
if (!input.TryGetProperty("tool_input", out var toolInput)
    || !toolInput.TryGetProperty("file_path", out var p)
    || p.GetString() is not { Length: > 0 } filePath)
{
    return 0;
}

var projectDir = Environment.GetEnvironmentVariable("CLAUDE_PROJECT_DIR")
    ?? (input.TryGetProperty("cwd", out var cwd) ? cwd.GetString() : null)
    ?? Directory.GetCurrentDirectory();
var fullPath = Path.GetFullPath(filePath, projectDir);
var relative = Path.GetRelativePath(projectDir, fullPath).Replace('\\', '/');
var extension = Path.GetExtension(fullPath).ToLowerInvariant();

if (relative.StartsWith(".claude/", StringComparison.Ordinal)
    || extension is not (".cs" or ".csproj" or ".props" or ".targets"))
{
    return 0;
}

var solution = Path.Combine(projectDir, "Staybook.slnx");
if (!File.Exists(solution))
{
    return 0;
}

if (extension == ".cs")
{
    // Whitespace only, in folder mode: no project load, so it takes about a second.
    Run(projectDir, "dotnet", "format", "whitespace", "--folder", "--include", relative);
}

// A .props or .targets file can affect every project, so build the solution for those.
var target = extension is ".props" or ".targets" ? solution : FindOwningProject(fullPath, projectDir) ?? solution;

var (exitCode, output) = Run(projectDir, "dotnet", "build", target, "-nologo", "-v", "q", "-clp:NoSummary");
if (exitCode == 0)
{
    return 0;
}

var errors = output.Split('\n')
    .Where(line => line.Contains("error", StringComparison.OrdinalIgnoreCase))
    .Select(line => line.Trim())
    .Distinct()
    .Take(30)
    .ToList();

Console.Error.WriteLine($"Build failed after editing {relative} ({Path.GetFileName(target)}):");
Console.Error.WriteLine(errors.Count > 0 ? string.Join(Environment.NewLine, errors) : output);
return 2;

static string? FindOwningProject(string fullPath, string projectDir)
{
    for (var dir = new DirectoryInfo(Path.GetDirectoryName(fullPath)!);
         dir is not null && dir.FullName.Length >= projectDir.Length;
         dir = dir.Parent)
    {
        var project = dir.GetFiles("*.csproj").FirstOrDefault();
        if (project is not null)
        {
            return project.FullName;
        }
    }

    return null;
}

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
