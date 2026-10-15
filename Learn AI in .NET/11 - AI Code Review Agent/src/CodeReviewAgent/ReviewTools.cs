using System.ComponentModel;

namespace CodeReviewAgent;

/// <summary>One problem the agent found in the code.</summary>
public sealed record Finding(string File, int Line, string Severity, string Message);

/// <summary>
/// The tools the agent can call. This is the heart of "tool calling": each method
/// becomes a function the LLM can invoke on its own during the agent loop.
///
/// LESSON — tools are how an LLM acts on the world.
/// A plain chat model can only produce text. Give it tools and it can DO things:
/// here, discover files, read them, and record findings. Microsoft.Extensions.AI's
/// function-invocation middleware runs the loop — model asks to call a tool, we
/// execute it, the result goes back to the model, repeat — until it's finished.
///
/// SECURITY — the file tools are sandboxed to <see cref="_rootDir"/>. We strip any
/// directory from the requested name so the model can't escape with "../../secrets".
/// </summary>
public sealed class ReviewTools(string rootDir, List<Finding> findings)
{
    private readonly string _rootDir = Path.GetFullPath(rootDir);

    [Description("List the code files available to review. Returns their file names.")]
    public string[] ListFiles() =>
        Directory.Exists(_rootDir)
            ? Directory.EnumerateFiles(_rootDir, "*.cs").Select(Path.GetFileName).OfType<string>().ToArray()
            : [];

    [Description("Read the full text of one code file so you can review it.")]
    public string ReadFile(
        [Description("The file name to read, e.g. UserService.cs")] string fileName)
    {
        // Path-traversal guard: keep only the file name, then re-root it.
        var safeName = Path.GetFileName(fileName);
        var fullPath = Path.Combine(_rootDir, safeName);

        return File.Exists(fullPath)
            ? File.ReadAllText(fullPath)
            : $"ERROR: no file named '{safeName}'.";
    }

    [Description("Record a code-review finding. Call this once per problem you identify.")]
    public string ReportIssue(
        [Description("The file the issue is in")] string file,
        [Description("The 1-based line number, or 0 if unknown")] int line,
        [Description("Severity: Critical, Warning, or Info")] string severity,
        [Description("A clear, specific description of the problem and how to fix it")] string message)
    {
        findings.Add(new Finding(Path.GetFileName(file), line, severity, message));
        return "recorded";
    }
}
