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

    // Timeout.InfiniteTimeSpan is -1 ms, so the positive check also rejects it.
    internal static void ValidateTimeout(TimeSpan timeout, string parameterName)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, timeout, "The Servo timeout must be positive and finite.");
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
