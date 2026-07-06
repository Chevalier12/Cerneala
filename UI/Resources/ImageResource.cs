using Cerneala.Drawing;

namespace Cerneala.UI.Resources;

public sealed class ImageResource
{
    private readonly IDrawImage? image;
    private readonly string? path;

    public ImageResource(IDrawImage image)
    {
        this.image = image ?? throw new ArgumentNullException(nameof(image));
    }

    public ImageResource(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Image path cannot be empty.", nameof(path));
        }

        this.path = path;
    }

    public string Identity => path ?? $"embedded:{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(image!)}";

    public bool IsPathBacked => path is not null;

    public string? Path => path;

    public bool HasEmbeddedImage => image is not null;

    public IDrawImage Resolve(IImageLoader? loader = null)
    {
        if (image is not null)
        {
            return image;
        }

        IDrawImage loadedImage = (loader ?? throw new InvalidOperationException("An image loader is required for path-backed image resources.")).Load(path!);
        return loadedImage ?? throw new InvalidOperationException("Image loader returned a null image.");
    }
}
