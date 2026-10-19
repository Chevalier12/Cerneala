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
        catch (OperationCanceledException exception) when (
            !cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
        {
            string message = $"The Servo operation exceeded its {timeout} timeout.";
            throw new ServoTimeoutException(exception is ServoTimeoutDiagnosticException diagnostic
                ? $"{message} {diagnostic.Message}"
                : message);
        }
    }
}

/// <summary>
/// Cancellation raised by a wait whose last evaluation explains why it was still unsatisfied.
/// <see cref="ServoOperation"/> appends the message to the timeout failure.
/// </summary>
internal sealed class ServoTimeoutDiagnosticException : OperationCanceledException
{
    internal ServoTimeoutDiagnosticException(
        string diagnostic,
        Exception innerException,
        CancellationToken cancellationToken)
        : base(diagnostic, innerException, cancellationToken)
    {
    }
}
