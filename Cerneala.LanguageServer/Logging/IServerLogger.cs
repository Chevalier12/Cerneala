namespace Cerneala.LanguageServer.Logging;

internal interface IServerLogger
{
    ServerTraceLevel TraceLevel { get; }

    void SetTraceLevel(ServerTraceLevel traceLevel);

    void Info(string eventName, params (string Name, object? Value)[] properties);

    void Critical(string eventName, params (string Name, object? Value)[] properties);
}
