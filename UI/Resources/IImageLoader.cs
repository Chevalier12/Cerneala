using Cerneala.Drawing;

namespace Cerneala.UI.Resources;

public interface IImageLoader
{
    IDrawImage Load(string path);
}
