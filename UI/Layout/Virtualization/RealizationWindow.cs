namespace Cerneala.UI.Layout.Virtualization;

public readonly record struct RealizationWindow(int StartIndex, int EndIndexExclusive)
{
    public static RealizationWindow Empty { get; } = new(0, 0);

    public int Count => Math.Max(0, EndIndexExclusive - StartIndex);

    public bool IsEmpty => Count == 0;

    public bool Contains(int index)
    {
        return index >= StartIndex && index < EndIndexExclusive;
    }

    public static RealizationWindow Create(int itemCount, int startIndex, int endIndexExclusive)
    {
        if (itemCount <= 0)
        {
            return Empty;
        }

        int start = Math.Clamp(startIndex, 0, itemCount);
        int end = Math.Clamp(endIndexExclusive, start, itemCount);
        return new RealizationWindow(start, end);
    }
}
