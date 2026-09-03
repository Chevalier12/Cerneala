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
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The Servo timeout must be positive and finite.");
            }

            defaultTimeout = value;
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
