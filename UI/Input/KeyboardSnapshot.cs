namespace Cerneala.UI.Input;

public sealed class KeyboardSnapshot
{
    private readonly IReadOnlySet<InputKey> downKeys;

    private KeyboardSnapshot(IReadOnlySet<InputKey> downKeys)
    {
        this.downKeys = downKeys;
    }

    public static KeyboardSnapshot Empty { get; } = new(new HashSet<InputKey>());

    public static KeyboardSnapshot FromDownKeys(IEnumerable<InputKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        return new KeyboardSnapshot(keys.Where(key => key is not InputKey.None and not InputKey.Unknown).ToHashSet());
    }

    public bool IsDown(InputKey key)
    {
        return downKeys.Contains(key);
    }
}
