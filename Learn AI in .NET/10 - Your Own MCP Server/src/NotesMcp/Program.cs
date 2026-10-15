using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotesMcp.Services;

// =============================================================================
// Your Own MCP Server — Expert project #10
// Focus: Expose your own API as MCP tools for Claude, GitHub Copilot, Cursor.
//
// This is a "Notes" service exposed over the Model Context Protocol using the
// STDIO transport: the AI client launches this process and talks to it over
// stdin/stdout. See the README for how to register it in VS Code / Claude Desktop.
// =============================================================================

var builder = Host.CreateApplicationBuilder(args);

// CRITICAL for stdio transport: stdout is the PROTOCOL channel (JSON-RPC).
// All logging must go to stderr, or it corrupts the protocol and the client
// disconnects. This one line is the most common "my MCP server won't connect" fix.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

// "Your API": a normal singleton service, nothing AI-specific about it.
builder.Services.AddSingleton<NoteStore>();

// Register the MCP server, use stdio, and auto-discover [McpServerToolType] classes.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
