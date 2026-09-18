using Cerneala.Drawing;

namespace Cerneala.UI.Resources;

/// <summary>Loads independently owned images without blocking the calling rendering thread.</summary>
public interface IAsyncImageLoader : IImageLoader
{
    ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default);
}
