using Cerneala.Drawing.Text;
using Cerneala.UI.Hosting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame;

public sealed class MonoGameDrawingBackend : IDrawingBackend, IDisposable
{
    private static readonly Rectangle EmptyClip = new(0, 0, 0, 0);

    private readonly Stack<Rectangle> _clipStack = new();
    private readonly SpriteBatch _spriteBatch;
    private readonly Dictionary<TextTextureKey, Texture2D> _textTextureCache = new();
    private readonly Texture2D _whitePixel;
    private readonly SkiaTextRasterizer? _textRasterizer;
    private float coordinateScale = 1;

    public MonoGameDrawingBackend(SpriteBatch spriteBatch, Texture2D whitePixel, SkiaTextRasterizer? textRasterizer = null)
    {
        _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        _whitePixel = whitePixel ?? throw new ArgumentNullException(nameof(whitePixel));
        _textRasterizer = textRasterizer;
    }

    public static RasterizerState ScissorRasterizerState { get; } = new()
    {
        ScissorTestEnable = true
    };

    public float CoordinateScale
    {
        get => coordinateScale;
        set
        {
            UiCoordinateMapper.ValidateScale(value);
            coordinateScale = value;
        }
    }

    public void Render(DrawCommandList commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        foreach (DrawCommand command in commands)
        {
            RenderCommand(command);
        }
    }

    private void RenderCommand(DrawCommand command)
    {
        switch (command.Kind)
        {
            case DrawCommandKind.FillRectangle:
                FillRectangle(command.Rect, command.Color);
                break;

            case DrawCommandKind.DrawRectangle:
                DrawRectangle(command.Rect, command.Color, command.Thickness);
                break;

            case DrawCommandKind.FillEllipse:
                FillEllipse(command.Rect, command.Color);
                break;

            case DrawCommandKind.DrawEllipse:
                DrawEllipse(command.Rect, command.Color, command.Thickness);
                break;

            case DrawCommandKind.DrawLine:
                DrawLine(command.Position, command.EndPoint, command.Color, command.Thickness);
                break;

            case DrawCommandKind.DrawImage:
                DrawImage(command);
                break;

            case DrawCommandKind.DrawText:
                DrawText(command);
                break;

            case DrawCommandKind.PushClip:
                PushClip(command.Rect);
                break;

            case DrawCommandKind.PopClip:
                PopClip();
                break;

            default:
                throw new InvalidOperationException($"Unsupported draw command: {command.Kind}");
        }
    }

    private void FillRectangle(DrawRect rect, DrawColor color)
    {
        _spriteBatch.Draw(_whitePixel, ToRectangle(rect), ToColor(color));
    }

    private void DrawRectangle(DrawRect rect, DrawColor color, float thickness)
    {
        int lineThickness = Math.Max(1, (int)MathF.Round(thickness));
        Rectangle bounds = ToRectangle(rect);
        Color monoGameColor = ToColor(color);

        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, lineThickness), monoGameColor);
        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Left, bounds.Bottom - lineThickness, bounds.Width, lineThickness), monoGameColor);
        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Left, bounds.Top, lineThickness, bounds.Height), monoGameColor);
        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Right - lineThickness, bounds.Top, lineThickness, bounds.Height), monoGameColor);
    }

    private void FillEllipse(DrawRect rect, DrawColor color)
    {
        Rectangle bounds = ToRectangle(rect);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        Color monoGameColor = ToColor(color);
        float radiusX = bounds.Width / 2f;
        float radiusY = bounds.Height / 2f;
        float centerY = bounds.Top + radiusY;

        for (int y = 0; y < bounds.Height; y++)
        {
            float normalizedY = ((bounds.Top + y + 0.5f) - centerY) / radiusY;
            float span = MathF.Sqrt(MathF.Max(0, 1 - (normalizedY * normalizedY))) * radiusX;
            int left = (int)MathF.Round(bounds.Left + radiusX - span);
            int right = (int)MathF.Round(bounds.Left + radiusX + span);
            int width = Math.Max(1, right - left);
            _spriteBatch.Draw(_whitePixel, new Rectangle(left, bounds.Top + y, width, 1), monoGameColor);
        }
    }

    private void DrawEllipse(DrawRect rect, DrawColor color, float thickness)
    {
        int lineThickness = Math.Max(1, (int)MathF.Round(thickness));
        Rectangle bounds = ToRectangle(rect);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        DrawEllipseRing(bounds, ToColor(color), lineThickness);
    }

    private void DrawEllipseRing(Rectangle bounds, Color color, int thickness)
    {
        float radiusX = bounds.Width / 2f;
        float radiusY = bounds.Height / 2f;
        float centerX = bounds.Left + radiusX;
        float centerY = bounds.Top + radiusY;
        int segments = Math.Max(24, (int)MathF.Ceiling(MathF.PI * MathF.Max(radiusX, radiusY) / 2f));
        Vector2 previous = new(centerX + radiusX, centerY);

        for (int i = 1; i <= segments; i++)
        {
            float angle = MathHelper.TwoPi * i / segments;
            Vector2 next = new(centerX + (MathF.Cos(angle) * radiusX), centerY + (MathF.Sin(angle) * radiusY));
            DrawLine(previous, next, color, thickness);
            previous = next;
        }
    }

    private void DrawLine(DrawPoint start, DrawPoint end, DrawColor color, float thickness)
    {
        DrawLine(ToVector2(start), ToVector2(end), ToColor(color), Math.Max(1, (int)MathF.Round(thickness)));
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length <= 0)
        {
            _spriteBatch.Draw(_whitePixel, new Rectangle((int)MathF.Round(start.X), (int)MathF.Round(start.Y), thickness, thickness), color);
            return;
        }

        float angle = MathF.Atan2(delta.Y, delta.X);
        _spriteBatch.Draw(
            _whitePixel,
            start,
            null,
            color,
            angle,
            Vector2.Zero,
            new Vector2(length, thickness),
            SpriteEffects.None,
            0);
    }

    private void DrawImage(DrawCommand command)
    {
        if (command.Image is not MonoGameImage image)
        {
            throw new InvalidOperationException("DrawImage requires a MonoGameImage when using MonoGameDrawingBackend.");
        }

        _spriteBatch.Draw(image.Texture, ToRectangle(command.Rect), ToColor(command.Color));
    }

    private void DrawText(DrawCommand command)
    {
        if (_textRasterizer is null || command.TextRun is null)
        {
            return;
        }

        TextTextureKey key = TextTextureKey.From(command.TextRun, command.Color);

        if (!_textTextureCache.TryGetValue(key, out Texture2D? texture))
        {
            RasterizedText text = _textRasterizer.Rasterize(command.TextRun, command.Color);
            texture = new Texture2D(_spriteBatch.GraphicsDevice, text.Width, text.Height);
            texture.SetData(text.RgbaPixels);
            _textTextureCache.Add(key, texture);
        }

        _spriteBatch.Draw(texture, ToVector2(command.Position), Color.White);
    }

    private void PushClip(DrawRect rect)
    {
        GraphicsDevice graphicsDevice = _spriteBatch.GraphicsDevice;
        Rectangle previousClip = _clipStack.Count == 0
            ? new Rectangle(0, 0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height)
            : graphicsDevice.ScissorRectangle;
        Rectangle requestedClip = ToRectangle(rect);

        _clipStack.Push(previousClip);
        graphicsDevice.ScissorRectangle = Intersect(previousClip, requestedClip);
    }

    private void PopClip()
    {
        if (_clipStack.Count == 0)
        {
            return;
        }

        _spriteBatch.GraphicsDevice.ScissorRectangle = _clipStack.Pop();
    }

    public void Dispose()
    {
        foreach (Texture2D texture in _textTextureCache.Values)
        {
            texture.Dispose();
        }

        _textTextureCache.Clear();
    }

    private static Rectangle ToRectangle(DrawRect rect)
    {
        return new Rectangle(
            (int)MathF.Round(rect.X),
            (int)MathF.Round(rect.Y),
            (int)MathF.Round(rect.Width),
            (int)MathF.Round(rect.Height));
    }

    private static Color ToColor(DrawColor color)
    {
        return new Color(color.R, color.G, color.B, color.A);
    }

    private static Vector2 ToVector2(DrawPoint point)
    {
        return new Vector2(point.X, point.Y);
    }

    private static Rectangle Intersect(Rectangle first, Rectangle second)
    {
        int left = Math.Max(first.Left, second.Left);
        int top = Math.Max(first.Top, second.Top);
        int right = Math.Min(first.Right, second.Right);
        int bottom = Math.Min(first.Bottom, second.Bottom);

        if (right <= left || bottom <= top)
        {
            return EmptyClip;
        }

        return new Rectangle(left, top, right - left, bottom - top);
    }

    private readonly record struct TextTextureKey(
        string Text,
        IDrawFont Font,
        float FontSize,
        DrawColor Color)
    {
        public static TextTextureKey From(DrawTextRun textRun, DrawColor color)
        {
            return new TextTextureKey(textRun.Text, textRun.Font, textRun.Size, color);
        }
    }
}
