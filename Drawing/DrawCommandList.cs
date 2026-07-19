using System.Collections;

namespace Cerneala.Drawing;

public sealed class DrawCommandList : IReadOnlyList<DrawCommand>
{
    private readonly List<DrawCommand> _commands = new();
    private long version;

    public int Count => _commands.Count;

    public DrawCommand this[int index] => _commands[index];

    public long Version => version;

    public void Add(DrawCommand command)
    {
        _commands.Add(command);
        unchecked
        {
            version++;
        }
    }

    public void Clear()
    {
        _commands.Clear();
        unchecked
        {
            version++;
        }
    }

    internal void ReplaceAt(int index, DrawCommand command)
    {
        _commands[index] = command;
        unchecked
        {
            version++;
        }
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
