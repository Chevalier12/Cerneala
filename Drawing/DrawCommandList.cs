using System.Collections;

namespace Cerneala.Drawing;

public sealed class DrawCommandList : IReadOnlyList<DrawCommand>
{
    private readonly List<DrawCommand> _commands = new();
    private long version;
    // Optional identities of commands replayed unchanged by retained scene
    // nodes, parallel to the commands once any has been recorded.
    private List<RetainedCommandKey>? retainedKeys;

    public int Count => _commands.Count;

    public DrawCommand this[int index] => _commands[index];

    // Analysis reads every command each frame; avoid copying the large struct.
    internal ref readonly DrawCommand ItemRef(int index) =>
        ref System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_commands)[index];

    public long Version => version;

    public void Add(DrawCommand command)
    {
        _commands.Add(command);
        retainedKeys?.Add(default);
        unchecked
        {
            version++;
        }
    }

    public void Clear()
    {
        _commands.Clear();
        retainedKeys?.Clear();
        unchecked
        {
            version++;
        }
    }

    internal void ReplaceAt(int index, DrawCommand command)
    {
        _commands[index] = command;
        if (retainedKeys is not null) { retainedKeys[index] = default; }
        unchecked
        {
            version++;
        }
    }

    internal void AddRetained(in DrawCommand command, RetainedCommandKey key)
    {
        if (retainedKeys is null)
        {
            retainedKeys = new List<RetainedCommandKey>(Math.Max(_commands.Capacity, 16));
            for (int index = 0; index < _commands.Count; index++)
            {
                retainedKeys.Add(default);
            }
        }

        _commands.Add(command);
        retainedKeys.Add(key);
        unchecked
        {
            version++;
        }
    }

    internal RetainedCommandKey GetRetainedKey(int index) =>
        retainedKeys is null ? default : retainedKeys[index];

    internal void Truncate(int count)
    {
        if (count < 0 || count > _commands.Count) { throw new ArgumentOutOfRangeException(nameof(count)); }
        if (count == _commands.Count) { return; }
        _commands.RemoveRange(count, _commands.Count - count);
        retainedKeys?.RemoveRange(count, retainedKeys.Count - count);
        unchecked { version++; }
    }

    public IEnumerator<DrawCommand> GetEnumerator()
    {
        return _commands.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

// Identifies a command value a retained owner recorded: the owner issues a
// new version whenever the command it records changes, so an equal key
// denotes an identical command.
internal readonly record struct RetainedCommandKey(object? Owner, long Version)
{
    internal bool IsSet => Owner is not null;

    internal bool Matches(RetainedCommandKey other) =>
        Owner is not null && ReferenceEquals(Owner, other.Owner) && Version == other.Version;
}
