using Cerneala.Drawing;

namespace Cerneala.UI.Rendering;

public sealed class DrawCommandListPool
{
    private readonly Stack<DrawCommandList> available = new();

    public DrawCommandListPool(int maxRetained = 32)
    {
        if (maxRetained < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetained), "Pool capacity must be non-negative.");
        }

        MaxRetained = maxRetained;
    }

    public int MaxRetained { get; }

    public int AvailableCount => available.Count;

    public DrawCommandList Rent()
    {
        return available.Count == 0 ? new DrawCommandList() : available.Pop();
    }

    public void Return(DrawCommandList commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        commands.Clear();
        if (available.Count < MaxRetained)
        {
            available.Push(commands);
        }
    }
}
