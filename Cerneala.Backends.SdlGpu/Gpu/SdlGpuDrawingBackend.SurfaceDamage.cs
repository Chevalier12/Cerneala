using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Backends.SdlGpu;

internal sealed partial class SdlGpuDrawingBackend
{
    private static SdlRect? ResolveSurfaceDamage(
        SdlGpuRenderSurfaceState surface,
        DrawCommandStateAnalysis next,
        Color clearColor)
    {
        SdlRect bounds = new(0, 0, surface.PixelWidth, surface.PixelHeight);
        IReadOnlyList<DrawCommandStateEntry>? previous = surface.RetainedEntries;
        if (previous is null || surface.RetainedClearColor != clearColor)
        {
            return bounds;
        }

        IReadOnlyList<DrawCommandStateEntry> current = next.Entries;
        int prefix = 0;
        int sharedCount = Math.Min(previous.Count, current.Count);
        while (prefix < sharedCount && SurfaceEntryEquals(previous[prefix], current[prefix]))
        {
            prefix++;
        }
        if (prefix == previous.Count && prefix == current.Count)
        {
            return null;
        }
        // The existing shared command metadata owns bounds and state identity.
        // Do not attempt to clip/reorder compositing or Prism scopes piecemeal.
        if (previous.Any(static entry => entry.IsContextSensitive) ||
            current.Any(static entry => entry.IsContextSensitive))
        {
            return bounds;
        }

        int previousEnd = previous.Count - 1;
        int currentEnd = current.Count - 1;
        while (previousEnd >= prefix && currentEnd >= prefix &&
            SurfaceEntryEquals(previous[previousEnd], current[currentEnd]))
        {
            previousEnd--;
            currentEnd--;
        }

        SdlRect? damage = null;
        for (int index = prefix; index <= previousEnd; index++)
        {
            Include(previous[index]);
        }
        for (int index = prefix; index <= currentEnd; index++)
        {
            Include(current[index]);
        }
        return damage;

        void Include(DrawCommandStateEntry entry)
        {
            SdlRect candidate = IntersectScissor(SurfaceCommandBounds(entry, bounds), bounds);
            if (candidate.Width <= 0 || candidate.Height <= 0)
            {
                return;
            }
            if (damage is not SdlRect existing)
            {
                damage = candidate;
                return;
            }
            int left = Math.Min(existing.X, candidate.X);
            int top = Math.Min(existing.Y, candidate.Y);
            int right = Math.Max(existing.X + existing.Width, candidate.X + candidate.Width);
            int bottom = Math.Max(existing.Y + existing.Height, candidate.Y + candidate.Height);
            damage = new SdlRect(left, top, right - left, bottom - top);
        }
    }

    private static bool SurfaceEntryEquals(DrawCommandStateEntry left, DrawCommandStateEntry right) =>
        left.Metadata?.RetainedIdentity.Equals(right.Metadata?.RetainedIdentity) == true;

    private static SdlRect SurfaceCommandBounds(DrawCommandStateEntry entry, SdlRect surfaceBounds) =>
        entry.Bounds is DrawRect bounds ? ToScissor(bounds, 1) : surfaceBounds;
}
