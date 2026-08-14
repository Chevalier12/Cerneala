using Cerneala.LanguageServer.Logging;
using Cerneala.LanguageServer.Protocol;
using Nerdbank.Streams;
using StreamJsonRpc;
using System.Threading.Channels;

namespace Cerneala.Tests.LanguageServer;

internal sealed class ProtocolTestClient : IAsyncDisposable
{
    private readonly Stream clientStream;
    private readonly Stream serverStream;
    private readonly Task<int> serverTask;
    private readonly ProtocolNotificationSink notifications;

    private ProtocolTestClient(
        Stream clientStream,
        Stream serverStream,
        Task<int> serverTask,
        JsonRpc rpc,
        ProtocolNotificationSink notifications)
    {
        this.clientStream = clientStream;
        this.serverStream = serverStream;
        this.serverTask = serverTask;
        this.notifications = notifications;
        Rpc = rpc;
    }

    public JsonRpc Rpc { get; }

    public static ProtocolTestClient Start(TextWriter? logWriter = null)
    {
        (Stream clientStream, Stream serverStream) = FullDuplexStream.CreatePair();
        StructuredServerLogger logger = new(logWriter ?? TextWriter.Null);
        Task<int> serverTask = LanguageServerHost.RunAsync(
            serverStream,
            serverStream,
            logger,
            CancellationToken.None);

        ProtocolNotificationSink notifications = new();
        JsonRpc rpc = new(new HeaderDelimitedMessageHandler(clientStream, new SystemTextJsonFormatter()));
        rpc.AddLocalRpcTarget(notifications, new JsonRpcTargetOptions
        {
            AllowNonPublicInvocation = false,
            ClientRequiresNamedArguments = true,
            UseSingleObjectParameterDeserialization = true
        });
        rpc.StartListening();
        return new ProtocolTestClient(clientStream, serverStream, serverTask, rpc, notifications);
    }

    public async Task<InitializeResult> InitializeAsync(
        CancellationToken cancellationToken,
        string? workspacePath = null) =>
        await Rpc.InvokeWithParameterObjectAsync<InitializeResult>(
            "initialize",
            new InitializeParams
            {
                ProcessId = Environment.ProcessId,
                RootUri = workspacePath is null
                    ? null
                    : new Uri(Path.GetDirectoryName(Path.GetFullPath(workspacePath))!).AbsoluteUri,
                Capabilities = new { },
                InitializationOptions = workspacePath is null
                    ? null
                    : new CernealaInitializationOptions { SolutionPath = workspacePath }
            },
            cancellationToken).ConfigureAwait(false);

    public Task<PublishDiagnosticsParams> WaitForDiagnosticsAsync(
        Func<PublishDiagnosticsParams, bool> predicate,
        CancellationToken cancellationToken) =>
        notifications.WaitAsync(predicate, cancellationToken);

    public async Task<int> StopAsync(CancellationToken cancellationToken)
    {
        await Rpc.InvokeWithCancellationAsync<object?>("shutdown", [], cancellationToken).ConfigureAwait(false);
        await Rpc.NotifyAsync("exit").ConfigureAwait(false);
        return await serverTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        Rpc.Dispose();
        await clientStream.DisposeAsync().ConfigureAwait(false);
        await serverStream.DisposeAsync().ConfigureAwait(false);
    }

    private sealed class ProtocolNotificationSink
    {
        private readonly Channel<PublishDiagnosticsParams> diagnostics =
            Channel.CreateUnbounded<PublishDiagnosticsParams>();

        [JsonRpcMethod("textDocument/publishDiagnostics", UseSingleObjectParameterDeserialization = true)]
        public void PublishDiagnostics(PublishDiagnosticsParams notification) =>
            diagnostics.Writer.TryWrite(notification);

        public async Task<PublishDiagnosticsParams> WaitAsync(
            Func<PublishDiagnosticsParams, bool> predicate,
            CancellationToken cancellationToken)
        {
            await foreach (PublishDiagnosticsParams notification in diagnostics.Reader.ReadAllAsync(cancellationToken))
            {
                if (predicate(notification))
                {
                    return notification;
                }
            }

            throw new InvalidOperationException("The diagnostics notification channel was completed.");
        }
    }
}
