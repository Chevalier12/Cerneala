namespace Cerneala.UI.Servo;

public sealed class ServoOptions
{
    private static readonly TimeSpan StandardTimeout = TimeSpan.FromSeconds(5);

    private TimeSpan defaultTimeout = StandardTimeout;

    public TimeSpan DefaultTimeout
    {
        get => defaultTimeout;
        set
        {
            ValidateTimeout(value, nameof(value));
            defaultTimeout = value;
        }
    }

    // The largest delay CancellationTokenSource.CancelAfter and Task.Delay accept: 4294967294 ms, about 49.7 days.
    // ServoOperation arms every deadline with CancelAfter, so a longer timeout could not run any operation.
    private static readonly TimeSpan MaxTimeout = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    // Timeout.InfiniteTimeSpan is -1 ms, so the positive check also rejects it.
    internal static void ValidateTimeout(TimeSpan timeout, string parameterName)
    {
        if (timeout <= TimeSpan.Zero || timeout > MaxTimeout)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                timeout,
                $"The Servo timeout must be positive and at most {MaxTimeout} (4294967294 ms).");
        }
    }

    internal static ServoOptions Copy(ServoOptions? source)
    {
        return new ServoOptions
        {
            DefaultTimeout = source?.DefaultTimeout ?? StandardTimeout
        };
    }
}
