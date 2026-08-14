using System.Text.Json;

namespace Cerneala.LanguageServer.Logging;

internal sealed class StructuredServerLogger(TextWriter writer) : IServerLogger
{
    private readonly Lock sync = new();

    public ServerTraceLevel TraceLevel { get; private set; } = ServerTraceLevel.Messages;

    public static StructuredServerLogger CreateForStandardError() => new(Console.Error);

    public void SetTraceLevel(ServerTraceLevel traceLevel) => TraceLevel = traceLevel;

    public void Info(string eventName, params (string Name, object? Value)[] properties)
    {
        if (TraceLevel != ServerTraceLevel.Off)
        {
            Write("info", eventName, properties);
        }
    }

    public void Critical(string eventName, params (string Name, object? Value)[] properties) =>
        Write("critical", eventName, properties);

    private void Write(string level, string eventName, (string Name, object? Value)[] properties)
    {
        Dictionary<string, object?> payload = new(StringComparer.Ordinal)
        {
            ["timestampUtc"] = DateTimeOffset.UtcNow,
            ["level"] = level,
            ["event"] = eventName
        };

        foreach ((string name, object? value) in properties)
        {
            payload[name] = value;
        }

        string line = JsonSerializer.Serialize(payload);
        lock (sync)
        {
            writer.WriteLine(line);
            writer.Flush();
        }
    }
}
