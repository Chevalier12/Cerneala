using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Ri.Mcp;

if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine(Help.Text);
    return 0;
}

if (args.Contains("--version", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("ri-mcp 0.1.0");
    return 0;
}

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<RoslynMcpTools>();

await builder.Build().RunAsync().ConfigureAwait(false);
return 0;

internal static class Help
{
    public const string Text = """
        ri-mcp - local Roslyn Repo Indexer MCP server

        Usage:
          ri-mcp --help
          ri-mcp --version
          ri-mcp

        Transport:
          stdio

        Tools:
          roslyn_doctor, roslyn_index, roslyn_status, roslyn_search, roslyn_read, roslyn_pread, roslyn_goto, roslyn_refs, roslyn_suggest
        """;
}
