using Cerneala.LanguageServer.Logging;
using StreamJsonRpc;

namespace Cerneala.LanguageServer.Protocol;

internal static class LanguageServerHost
{
    public static async Task<int> RunAsync(
        Stream input,
        Stream output,
        IServerLogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(logger);

        SystemTextJsonFormatter formatter = new();
        await using HeaderDelimitedMessageHandler handler = new(output, input, formatter);
        using JsonRpc rpc = new(handler);
        LanguageServerEndpoint endpoint = new(logger);
        endpoint.AttachClient(rpc);
        rpc.AddLocalRpcTarget(endpoint, new JsonRpcTargetOptions
        {
            AllowNonPublicInvocation = false,
            ClientRequiresNamedArguments = true,
            DisposeOnDisconnect = false,
            UseSingleObjectParameterDeserialization = true
        });

        logger.Info("server.started", ("protocolVersion", "3.17"));
        rpc.StartListening();

        try
        {
            Task cancellationTask = Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            Task completed = await Task.WhenAny(endpoint.ExitTask, rpc.Completion, cancellationTask).ConfigureAwait(false);
            if (completed == cancellationTask)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (completed == endpoint.ExitTask)
            {
                return await endpoint.ExitTask.ConfigureAwait(false);
            }

            logger.Info("server.disconnected");
            return rpc.Completion.IsFaulted ? 1 : 0;
        }
        finally
        {
            await endpoint.DisposeAsync().ConfigureAwait(false);
        }
    }
}
