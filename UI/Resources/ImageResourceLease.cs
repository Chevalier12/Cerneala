using Cerneala.Drawing;

namespace Cerneala.UI.Resources;

/// <summary>Keeps one acquired image alive until this acquisition is released.</summary>
public sealed class ImageResourceLease : IDisposable
{
    private readonly object gate = new();
    private Acquisition? acquisition;

    internal ImageResourceLease(
        IDrawImage image,
        Func<ImageResourceLease> retain,
        Action? release,
        object? token = null)
    {
        acquisition = new Acquisition(image, retain, release, token);
    }

    public IDrawImage Image => GetAcquisition().Image;

    /// <summary>Creates an independently releasable acquisition of the same image.</summary>
    public ImageResourceLease Retain()
    {
        lock (gate)
        {
            return GetAcquisition().Retain();
        }
    }

    public void Dispose()
    {
        Acquisition? released;
        lock (gate)
        {
            released = acquisition;
            acquisition = null;
        }
        // Disposal can run user/backend callbacks. Never run them under the lease lock.
        released?.Release?.Invoke();
    }

    internal object? Token => Volatile.Read(ref acquisition)?.Token;

    internal static ImageResourceLease Borrow(IDrawImage image) =>
        new(image, () => Borrow(image), release: null);

    private Acquisition GetAcquisition() =>
        Volatile.Read(ref acquisition) ?? throw new ObjectDisposedException(nameof(ImageResourceLease));

    private sealed record Acquisition(
        IDrawImage Image,
        Func<ImageResourceLease> Retain,
        Action? Release,
        object? Token);
}
