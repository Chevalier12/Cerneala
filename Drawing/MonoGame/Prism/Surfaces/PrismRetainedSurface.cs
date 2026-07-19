using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Surfaces;

// Ownership has left the transient pool. The retained owner must dispose this
// object and must not render into the finalized surface again.
internal sealed class PrismRetainedSurface : IDisposable
{
    private RenderTarget2D? surface;

    internal PrismRetainedSurface(
        PrismSurfaceKey key,
        RenderTarget2D surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        Key = key;
        this.surface = surface;
    }

    public PrismSurfaceKey Key { get; }

    public RenderTarget2D Surface =>
        surface ??
        throw new ObjectDisposedException(nameof(PrismRetainedSurface));

    public void Dispose()
    {
        RenderTarget2D? ownedSurface = surface;
        surface = null;
        ownedSurface?.Dispose();
    }
}
