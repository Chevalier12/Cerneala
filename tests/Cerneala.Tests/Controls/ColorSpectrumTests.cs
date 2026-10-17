using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls;

public sealed class ColorSpectrumTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    public void PressAndDragSelectThePointDrawnUnderThePointer(float scale)
    {
        UIRoot root = RootWithSpectrum(scale, out ColorSpectrum spectrum);
        ElementInputBridge bridge = new();
        (float pressX, float pressY) = DrawnPoint(spectrum, scale, 50, 50);
        (float dragX, float dragY) = DrawnPoint(spectrum, scale, 25, 75);
        (float releaseX, float releaseY) = DrawnPoint(spectrum, scale, 80, 10);

        bridge.Dispatch(root, PointerFrame(pressX, pressY, pressX, pressY, currentDown: true));

        Assert.Equal((0.5f, 0.5f), (spectrum.Saturation, spectrum.Value));

        bridge.Dispatch(root, PointerFrame(pressX, pressY, dragX, dragY, previousDown: true, currentDown: true));

        Assert.Equal((0.25f, 0.25f), (spectrum.Saturation, spectrum.Value));

        bridge.Dispatch(root, PointerFrame(dragX, dragY, releaseX, releaseY, previousDown: true, currentDown: true));
        bridge.Dispatch(root, PointerFrame(releaseX, releaseY, releaseX, releaseY, previousDown: true));

        Assert.Equal((0.8f, 0.9f), (spectrum.Saturation, spectrum.Value));
        Assert.False(bridge.PointerCaptureManager.HasCapture);
    }

    // The spectrum is arranged at a non-zero origin inside a parent that draws its content `scale` times
    // larger from the root origin, so the drawn spectrum starts at (10, 20) * scale.
    private static UIRoot RootWithSpectrum(float scale, out ColorSpectrum spectrum)
    {
        UIRoot root = new(400, 400);
        UIElement parent = new()
        {
            RenderTransform = new Transform(Matrix3x2.CreateScale(scale, scale)),
            RenderTransformOrigin = new LayoutPoint(0, 0)
        };
        spectrum = new ColorSpectrum();
        parent.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 200)));
        spectrum.Arrange(new ArrangeContext(new LayoutRect(10, 20, 100, 100)));
        parent.VisualChildren.Add(spectrum);
        root.VisualChildren.Add(parent);
        return root;
    }

    private static (float X, float Y) DrawnPoint(ColorSpectrum spectrum, float scale, float localX, float localY)
    {
        return ((spectrum.ArrangedBounds.X + localX) * scale, (spectrum.ArrangedBounds.Y + localY) * scale);
    }

    private static InputFrame PointerFrame(
        float previousX,
        float previousY,
        float currentX,
        float currentY,
        bool previousDown = false,
        bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(previousX, previousY);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(currentX, currentY);
        if (previousDown)
        {
            previous = previous.WithButton(InputMouseButton.Left, true);
        }

        if (currentDown)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }
}
