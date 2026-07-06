using Cerneala.Drawing;

namespace Cerneala.UI.Resources;

public sealed class ImageResourceCache : IDisposable
{
    private readonly IImageLoader? loader;
    private readonly Dictionary<string, IDrawImage> images = new(StringComparer.Ordinal);

    public ImageResourceCache(IImageLoader? loader)
    {
        this.loader = loader;
    }

    public IDrawImage Resolve(ImageResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (resource.HasEmbeddedImage)
        {
            return resource.Resolve();
        }

        if (!resource.IsPathBacked || resource.Path is null)
        {
            throw new InvalidOperationException("Image resource is not resolvable.");
        }

        if (images.TryGetValue(resource.Identity, out IDrawImage? cached))
        {
            return cached;
        }

        if (loader is null)
        {
            throw new InvalidOperationException("An image loader is required for path-backed image resources.");
        }

        IDrawImage loaded = resource.Resolve(loader);
        images.Add(resource.Identity, loaded);
        return loaded;
    }

    public void Remove(ImageResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        if (images.Remove(resource.Identity, out IDrawImage? image))
        {
            DisposeIfOwned(image);
        }
    }

    public void Clear()
    {
        foreach (IDrawImage image in images.Values)
        {
            DisposeIfOwned(image);
        }

        images.Clear();
    }

    public void Dispose()
    {
        Clear();
    }

    private static void DisposeIfOwned(IDrawImage image)
    {
        if (image is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
