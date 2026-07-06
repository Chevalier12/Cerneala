using Cerneala.Drawing.Text;
using Cerneala.UI.Elements;
using Cerneala.UI.Input.MonoGame;
using Cerneala.UI.Resources;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.UI.Hosting.MonoGame;

public sealed class MonoGameUiHostOptions
{
    public required SpriteBatch SpriteBatch { get; init; }

    public required Texture2D WhitePixel { get; init; }

    public UIRoot? Root { get; init; }

    public UiViewport Viewport { get; init; } = new(0, 0);

    public MonoGameInputSource? InputSource { get; init; }

    public MonoGameContentServices? ContentServices { get; init; }

    public IImageLoader? ImageLoader { get; init; }

    public IUiClock? Clock { get; init; }

    public SkiaTextRasterizer? TextRasterizer { get; init; }
}
