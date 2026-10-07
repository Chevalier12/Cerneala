namespace Cerneala.Tests.Timbre.Harness;

internal static class HarnessWait
{
    // Fixture timeouts are harness failures, never an audio RED.
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    public static async Task WithTimeout(Task task, TimeSpan? timeout, string failure)
    {
        Task completed = await Task.WhenAny(task, Task.Delay(timeout ?? DefaultTimeout)).ConfigureAwait(false);
        if (!ReferenceEquals(completed, task))
        {
            throw new TimeoutException($"Harness timeout: {failure}");
        }

        await task.ConfigureAwait(false);
    }

    public static async Task<T> WithTimeout<T>(Task<T> task, TimeSpan? timeout, string failure)
    {
        await WithTimeout((Task)task, timeout, failure).ConfigureAwait(false);
        return await task.ConfigureAwait(false);
    }
}
