namespace Cerneala.Drawing.MonoGame.Prism.Surfaces;

internal sealed class PrismSurfaceAllocationException :
    InvalidOperationException
{
    public PrismSurfaceAllocationException(
        PrismSurfaceKey key,
        Exception innerException)
        : base(
            $"A Prism surface could not be allocated for key '{key}'.",
            innerException)
    {
        Key = key;
    }

    public PrismSurfaceKey Key { get; }
}
