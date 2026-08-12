using Cerneala.Drawing;

namespace Cerneala.UI.Resources;

public interface IImageLoader
{
    IDrawImage Load(string path);

    IDrawImage Load(Stream stream)
    {
        throw new NotSupportedException("This image loader does not support stream-backed images.");
    }
}
