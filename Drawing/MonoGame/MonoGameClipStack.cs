using Microsoft.Xna.Framework;

namespace Cerneala.Drawing.MonoGame;

internal sealed class MonoGameClipStack
{
    private static readonly Rectangle EmptyClip = new(0, 0, 0, 0);
    private readonly Stack<Rectangle> previousClips = new();
    private readonly Rectangle initialClip;

    public MonoGameClipStack(Rectangle initialClip)
    {
        this.initialClip = initialClip;
        CurrentClip = initialClip;
    }

    public Rectangle CurrentClip { get; private set; }

    public int Depth => previousClips.Count;

    public void Push(Rectangle requestedClip)
    {
        previousClips.Push(CurrentClip);
        CurrentClip = Intersect(CurrentClip, requestedClip);
    }

    public Rectangle Pop()
    {
        if (previousClips.Count == 0)
        {
            return CurrentClip;
        }

        CurrentClip = previousClips.Pop();
        return CurrentClip;
    }

    public void Reset()
    {
        previousClips.Clear();
        CurrentClip = initialClip;
    }

    internal static Rectangle Intersect(Rectangle first, Rectangle second)
    {
        int left = Math.Max(first.Left, second.Left);
        int top = Math.Max(first.Top, second.Top);
        int right = Math.Min(first.Right, second.Right);
        int bottom = Math.Min(first.Bottom, second.Bottom);

        if (right <= left || bottom <= top)
        {
            return EmptyClip;
        }

        return new Rectangle(left, top, right - left, bottom - top);
    }
}
