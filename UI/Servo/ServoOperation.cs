namespace Cerneala.UI.Servo;

internal static class ServoOperation
{
    internal static async Task RunAsync(
        TimeSpan timeout,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        await RunAsync(
            timeout,
            async token =>
            {
                await operation(token).ConfigureAwait(false);
                return true;
            },
            cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<T> RunAsync<T>(
        TimeSpan timeout,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();
        using CancellationTokenSource timeoutSource = new();
        timeoutSource.CancelAfter(timeout);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);
        try
        {
            return await operation(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException canceled) when (
            !cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
        {
            string message = $"The Servo operation exceeded its {timeout} timeout.";
            // A retried action carries the failure it was retrying as the InnerException of its
            // cancellation (ServoActionEngine.DispatchWhenActionableAsync).
            if (canceled.InnerException is ServoException lastFailure)
            {
                throw new ServoTimeoutException($"{message} Last failure: {lastFailure.Message}", lastFailure);
            }

            throw new ServoTimeoutException(message);
        }
    }
}
