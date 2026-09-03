using Cerneala.UI.Hosting;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Hosting.Windowing;

internal readonly record struct WindowScreenshotRegion(int X, int Y, int Width, int Height)
{
    internal static bool TryCreate(LayoutRect bounds, UiViewport viewport, out WindowScreenshotRegion region)
    {
        if (!float.IsFinite(bounds.X) || !float.IsFinite(bounds.Y) ||
            !float.IsFinite(bounds.Width) || !float.IsFinite(bounds.Height) ||
            bounds.Width <= 0 || bounds.Height <= 0 ||
            !float.IsFinite(viewport.Width) || !float.IsFinite(viewport.Height) ||
            !float.IsFinite(viewport.Scale) || viewport.Width <= 0 ||
            viewport.Height <= 0 || viewport.Scale <= 0)
        {
            region = default;
            return false;
        }

        double framebufferWidth = Math.Ceiling(viewport.Width * viewport.Scale);
        double framebufferHeight = Math.Ceiling(viewport.Height * viewport.Scale);
        if (framebufferWidth <= 0 || framebufferHeight <= 0 ||
            framebufferWidth > int.MaxValue || framebufferHeight > int.MaxValue)
        {
            region = default;
            return false;
        }

        double left = Math.Clamp(Math.Floor(bounds.X * viewport.Scale), 0, framebufferWidth);
        double top = Math.Clamp(Math.Floor(bounds.Y * viewport.Scale), 0, framebufferHeight);
        double right = Math.Clamp(
            Math.Ceiling(((double)bounds.X + bounds.Width) * viewport.Scale), 0, framebufferWidth);
        double bottom = Math.Clamp(
            Math.Ceiling(((double)bounds.Y + bounds.Height) * viewport.Scale), 0, framebufferHeight);
        if (right <= left || bottom <= top)
        {
            region = default;
            return false;
        }

        region = new WindowScreenshotRegion(
            (int)left,
            (int)top,
            checked((int)(right - left)),
            checked((int)(bottom - top)));
        return true;
    }

    internal void ValidateWithin(int pixelWidth, int pixelHeight)
    {
        if (X < 0 || Y < 0 || Width <= 0 || Height <= 0 ||
            (long)X + Width > pixelWidth || (long)Y + Height > pixelHeight)
        {
            throw new ArgumentOutOfRangeException(
                nameof(WindowScreenshotRegion),
                "The screenshot region must be non-empty and contained by the framebuffer.");
        }
    }
}

internal static class WindowScreenshotPixels
{
    internal static WindowPreviewFrame CropRgba(WindowPreviewFrame frame, WindowScreenshotRegion region)
    {
        region.ValidateWithin(frame.PixelWidth, frame.PixelHeight);
        int stride = checked(region.Width * 4);
        byte[] pixels = new byte[checked(stride * region.Height)];
        for (int row = 0; row < region.Height; row++)
        {
            frame.Pixels.AsSpan(
                    checked(((region.Y + row) * frame.Stride) + (region.X * 4)),
                    stride)
                .CopyTo(pixels.AsSpan(row * stride, stride));
        }

        return new WindowPreviewFrame(pixels, region.Width, region.Height, stride);
    }
}
