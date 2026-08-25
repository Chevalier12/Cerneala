using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Hosting.MonoGame;

/// <summary>
/// Provides a compatibility name for backend-agnostic drawing content services used by MonoGame hosts.
/// </summary>
public sealed class MonoGameContentServices : DrawingContentServices
{
    public MonoGameContentServices(
        IFontSource? fontSource = null,
        SkiaTextRasterizer? textRasterizer = null,
        IImageLoader? imageLoader = null)
        : base(fontSource, textRasterizer, imageLoader)
    {
    }
}
