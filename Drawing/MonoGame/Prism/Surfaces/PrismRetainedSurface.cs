using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Surfaces;

// Ownership has left the transient pool. The retained owner must dispose this
// object and must not render into the finalized surface again.
internal sealed class PrismRetainedSurface : IDisposable
{
    private readonly PrismSurfaceMemoryAccountant accountant;
    private RenderTarget2D? surface;

    internal PrismRetainedSurface(
        PrismSurfaceKey key,
        RenderTarget2D surface,
        PrismSurfaceMemoryAccountant accountant,
        long byteCount)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(accountant);
        if (byteCount != key.CalculateByteSize())
        {
            throw new ArgumentOutOfRangeException(nameof(byteCount));
        }

        Key = key;
        this.surface = surface;
        this.accountant = accountant;
        ByteCount = byteCount;
    }

    public PrismSurfaceKey Key { get; }

    public long ByteCount { get; }

    public RenderTarget2D Surface
    {
        get
        {
            accountant.VerifyAccess();
            return surface ??
                throw new ObjectDisposedException(
                    nameof(PrismRetainedSurface));
        }
    }

    internal bool IsOwnedBy(
        PrismSurfaceMemoryAccountant candidate) =>
        ReferenceEquals(accountant, candidate);

    internal RenderTarget2D DetachToTransientOwner()
    {
        accountant.VerifyAccess();
        RenderTarget2D ownedSurface =
            surface ??
            throw new ObjectDisposedException(
                nameof(PrismRetainedSurface));
        accountant.TransferRetainedToTransient(ByteCount);
        surface = null;
        return ownedSurface;
    }

    public void Dispose()
    {
        accountant.VerifyAccess();
        RenderTarget2D? ownedSurface = surface;
        if (ownedSurface is null)
        {
            return;
        }

        accountant.ReleaseRetained(ByteCount);
        surface = null;
        ownedSurface.Dispose();
    }
}
