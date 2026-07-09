using System.Collections;

namespace Cerneala.Drawing;

public sealed class DrawCommandList : IReadOnlyList<DrawCommand>
{
    private readonly List<DrawCommand> _commands = new();

    public int Count => _commands.Count;

    public DrawCommand this[int index] => _commands[index];

    public void Add(DrawCommand command)
    {
        _commands.Add(command);
    }

    public void Clear()
    {
        _commands.Clear();
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
