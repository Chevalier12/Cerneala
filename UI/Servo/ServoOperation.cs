namespace Cerneala.UI.Servo;

internal static class ServoOperation
{
    private const string DefaultSubject = "The Servo operation";

    // describeSubject names the operation in a timeout message and runs only when the timeout expires.
    // Example: () => $"The Servo operation on target {target}" gives
    // The Servo operation on target Id="save" exceeded its 00:00:05 timeout.
    internal static async Task RunAsync(
        TimeSpan timeout,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken,
        Func<string>? describeSubject = null)
    {
        await RunAsync(
            timeout,
            async token =>
            {
                await operation(token).ConfigureAwait(false);
                return true;
            },
            cancellationToken,
            describeSubject).ConfigureAwait(false);
    }

    internal static async Task<T> RunAsync<T>(
        TimeSpan timeout,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken,
        Func<string>? describeSubject = null)
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
        catch (OperationCanceledException) when (
            !cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
        {
            throw new ServoTimeoutException(
                $"{describeSubject?.Invoke() ?? DefaultSubject} exceeded its {timeout} timeout.");
        }
    }
}
