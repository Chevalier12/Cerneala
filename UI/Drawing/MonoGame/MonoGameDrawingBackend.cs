using Cerneala.Drawing.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame;

public sealed class MonoGameDrawingBackend : IDrawingBackend
{
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _whitePixel;
    private readonly SkiaTextRasterizer? _textRasterizer;

    public MonoGameDrawingBackend(SpriteBatch spriteBatch, Texture2D whitePixel, SkiaTextRasterizer? textRasterizer = null)
    {
        _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        _whitePixel = whitePixel ?? throw new ArgumentNullException(nameof(whitePixel));
        _textRasterizer = textRasterizer;
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

            case DrawCommandKind.DrawImage:
                DrawImage(command);
                break;

            case DrawCommandKind.DrawText:
                DrawText(command);
                break;

            case DrawCommandKind.PushClip:
            case DrawCommandKind.PopClip:
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

        RasterizedText text = _textRasterizer.Rasterize(command.TextRun, command.Color);
        Texture2D texture = new(_spriteBatch.GraphicsDevice, text.Width, text.Height);
        texture.SetData(text.RgbaPixels);
        _spriteBatch.Draw(texture, ToVector2(command.Position), Color.White);
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
}
