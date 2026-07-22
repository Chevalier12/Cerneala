using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Surfaces;

internal readonly struct PrismScratchSurfaceLease : IDisposable
{
    private readonly PrismSurfacePool? pool;
    private readonly long generation;
    private readonly int scratchIndex;

    internal PrismScratchSurfaceLease(
        PrismSurfacePool pool,
        long generation,
        int scratchIndex,
        RenderTarget2D surface)
    {
        this.pool = pool;
        this.generation = generation;
        this.scratchIndex = scratchIndex;
        Surface = surface;
    }

    public RenderTarget2D Surface { get; }

    public void Dispose()
    {
        if (pool is null)
        {
            return;
        }

        pool.ReleaseScratch(
            generation,
            scratchIndex,
            Surface);
    }
}
