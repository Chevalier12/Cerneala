using Cerneala.Drawing;

namespace Cerneala.UI.Resources;

public sealed class ImageResource
{
    private static long nextRetainedIdentity;

    private readonly IDrawImage? image;
    private readonly string? path;
    private readonly long retainedIdentity;

    public ImageResource(IDrawImage image)
    {
        this.image = image ?? throw new ArgumentNullException(nameof(image));
        retainedIdentity = NextRetainedIdentity();
    }

    public ImageResource(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Image path cannot be empty.", nameof(path));
        }

        this.path = path;
        retainedIdentity = NextRetainedIdentity();
    }

    public string Identity => path ?? $"embedded:{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(image!)}";

    public bool IsPathBacked => path is not null;

    public string? Path => path;

    public bool HasEmbeddedImage => image is not null;

    internal long RetainedIdentity => retainedIdentity;

    public IDrawImage Resolve(IImageLoader? loader = null)
    {
        if (image is not null)
        {
            return image;
        }

        IDrawImage loadedImage = (loader ?? throw new InvalidOperationException("An image loader is required for path-backed image resources.")).Load(path!);
        return loadedImage ?? throw new InvalidOperationException("Image loader returned a null image.");
    }

    private static long NextRetainedIdentity()
    {
        long value = Interlocked.Increment(ref nextRetainedIdentity);
        if (value <= 0)
        {
            throw new InvalidOperationException(
                "Image resource identity space was exhausted.");
        }

        return value;
    }
}
