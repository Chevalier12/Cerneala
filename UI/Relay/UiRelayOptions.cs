namespace Cerneala.UI.Relay;

public sealed class UiRelayOptions
{
    private int maxCallbacksPerUpdate = 1024;

    public int MaxCallbacksPerUpdate
    {
        get => maxCallbacksPerUpdate;
        init => maxCallbacksPerUpdate = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "MaxCallbacksPerUpdate must be greater than zero.");
    }
}
