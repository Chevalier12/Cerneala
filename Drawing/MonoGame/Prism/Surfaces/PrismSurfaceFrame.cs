using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Surfaces;

internal readonly struct PrismSurfaceFrame : IDisposable
{
    private readonly PrismSurfacePool? pool;
    private readonly long generation;

    internal PrismSurfaceFrame(PrismSurfacePool pool, long generation)
    {
        this.pool = pool;
        this.generation = generation;
    }

    public void AdvanceToStep(
        int step,
        ReadOnlySpan<PrismSurfaceKey> surfaceKeys)
    {
        GetPool().AdvanceToStep(generation, step, surfaceKeys);
    }

    public void AdvanceToStep(
        int step,
        ReadOnlySpan<PrismSurfaceKey> surfaceKeys,
        ReadOnlySpan<bool> requiredSurfaces)
    {
        GetPool().AdvanceToStep(
            generation,
            step,
            surfaceKeys,
            requiredSurfaces);
    }

    public RenderTarget2D GetSurface(int executionIndex)
    {
        return GetPool().GetSurface(generation, executionIndex);
    }

    public PrismRetainedSurface PromoteToRetainedOwner(int executionIndex)
    {
        return GetPool().PromoteToRetainedOwner(generation, executionIndex);
    }

    internal bool IsOwnedBy(PrismSurfacePool candidate) =>
        ReferenceEquals(pool, candidate);

    internal PrismSurfaceKey ValidatePromotion(int executionIndex)
    {
        return GetPool().ValidatePromotion(
            generation,
            executionIndex);
    }

    public void Dispose()
    {
        pool?.EndFrame(generation);
    }

    private PrismSurfacePool GetPool()
    {
        return pool ??
            throw new InvalidOperationException(
                "The Prism surface frame token is not initialized.");
    }
}
