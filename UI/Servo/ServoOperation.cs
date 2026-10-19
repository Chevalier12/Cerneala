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
        catch (OperationCanceledException exception) when (
            !cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
        {
            string message = $"{describeSubject?.Invoke() ?? DefaultSubject} exceeded its {timeout} timeout.";
            if (exception is ServoTimeoutDiagnosticException diagnostic)
            {
                throw new ServoTimeoutException(
                    $"{message} {diagnostic.Message}",
                    diagnostic.InnerException as ServoException);
            }

            throw new ServoTimeoutException(message);
        }
    }
}

/// <summary>
/// Cancellation raised by a wait whose last evaluation explains why it was still unsatisfied.
/// <see cref="ServoOperation"/> appends the message to the timeout failure. When the inner exception
/// is a <see cref="ServoException"/>, the last failure of a retried target action, it also becomes
/// the timeout's inner exception.
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
