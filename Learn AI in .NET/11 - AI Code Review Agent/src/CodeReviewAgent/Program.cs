using System.Text.Json;
using CodeReviewAgent;
using Microsoft.Extensions.AI;
using OllamaSharp;

// =============================================================================
// AI Code Review Agent — Expert project #11
// Focus: The agent loop + tool calling.
//
// The agent is told to review the .cs files in the /sample folder. It then works
// AUTONOMOUSLY: it calls list_files, reads each file, and reports issues — the LLM
// decides which tools to call and when. Microsoft.Extensions.AI runs the loop.
//
// Prerequisite: Ollama with a TOOL-CAPABLE chat model, e.g.
//   ollama pull llama3.2
// =============================================================================

var ollamaUrl = new Uri(Environment.GetEnvironmentVariable("OLLAMA_URL") ?? "http://127.0.0.1:11434");
var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.2";

var sampleDir = Path.Combine(AppContext.BaseDirectory, "sample");
var findings = new List<Finding>();
var tools = new ReviewTools(sampleDir, findings);

Console.WriteLine("🕵️  AI Code Review Agent\n");

// --- Offline check: prove the tools work without any AI -----------------------
var files = tools.ListFiles();
Console.WriteLine($"Files available for review ({files.Length}):");
foreach (var f in files)
    Console.WriteLine($"  • {f}");
Console.WriteLine();

// --- Build the chat client WITH function invocation (this is the agent loop) --
// UseFunctionInvocation() is the middleware that turns tool-call requests from the
// model into real method calls and feeds the results back — looping until done.
IChatClient chat = new ChatClientBuilder(new OllamaApiClient(ollamaUrl, model))
    .UseFunctionInvocation()
    .Build();

var chatOptions = new ChatOptions
{
    Tools =
    [
        AIFunctionFactory.Create(tools.ListFiles),
        AIFunctionFactory.Create(tools.ReadFile),
        AIFunctionFactory.Create(tools.ReportIssue)
    ]
};

var messages = new List<ChatMessage>
{
    new(ChatRole.System, """
        You are a senior .NET code reviewer. Review every code file in the sample set.
        Workflow:
          1. Call list_files to see what's there.
          2. Call read_file for EACH file.
          3. For every real problem (bug, security flaw, performance issue, bad
             practice), call report_issue with a specific message and, if you can,
             a line number.
        Focus on genuine issues, not style nitpicks. When finished, give a short
        summary of what you found.
        """),
    new(ChatRole.User, "Please review all the code files and report the issues.")
};

Console.WriteLine($"Running the agent (model: {model})...\n");

try
{
    var response = await chat.GetResponseAsync(messages, chatOptions);

    Console.WriteLine("── Agent summary ──");
    Console.WriteLine(response.Text);
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"⚠️  Couldn't reach Ollama: {ex.Message}");
    Console.WriteLine($"    Try:  ollama pull {model}  then  ollama serve");
    Console.WriteLine("    (The file-listing above still proves the tools are wired up.)");
    return;
}

// --- Show everything the agent reported via the report_issue tool -------------
Console.WriteLine($"\n── Findings recorded via tool calls ({findings.Count}) ──");
foreach (var group in findings.GroupBy(f => f.File))
{
    Console.WriteLine($"\n{group.Key}");
    foreach (var f in group.OrderBy(x => x.Line))
        Console.WriteLine($"  [{f.Severity}] line {f.Line}: {f.Message}");
}

// Also dump as JSON — handy for feeding into CI or another tool.
Console.WriteLine("\n── JSON ──");
Console.WriteLine(JsonSerializer.Serialize(findings, new JsonSerializerOptions { WriteIndented = true }));
