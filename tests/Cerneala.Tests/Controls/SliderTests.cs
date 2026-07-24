using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.Tests.Controls;

public sealed class SliderTests
{
    [Fact]
    public void SliderDeclaresRequiredTrackTemplatePart()
    {
        TemplatePartAttribute part = Assert.Single(
            typeof(Slider).GetCustomAttributes(typeof(TemplatePartAttribute), inherit: true)
                .Cast<TemplatePartAttribute>(),
            candidate => candidate.Name == "PART_Track");

        Assert.Equal(typeof(Track), part.Type);
    }

    [Fact]
    public void SliderUsesTrackProvidedByComponentTemplate()
    {
        Track templateTrack = new();
        ComponentTemplate<Slider> template = new("Slider.Custom", context =>
        {
            context.RequirePart("PART_Track", templateTrack);
            return templateTrack;
        });
        Slider slider = new()
        {
            ComponentTemplate = template,
            Minimum = 0,
            Maximum = 100,
            Value = 25
        };

        slider.Measure(new MeasureContext(new LayoutSize(110, 20)));
        slider.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));
        templateTrack.Value = 75;

        Assert.Same(templateTrack, slider.Track);
        Assert.Equal(75, slider.Value);
    }

    [Fact]
    public void SliderValueFollowsThumbDrag()
    {
        UIRoot root = new(200, 100);
        Slider slider = new()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0
        };
        slider.Measure(new MeasureContext(new LayoutSize(110, 20)));
        slider.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));
        root.VisualChildren.Add(slider);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(5, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(5, 5, 55, 5, previousDown: true, currentDown: true));

        Assert.Equal(50, slider.Value);
    }

    [Fact]
    public void ClickingSliderTrackMovesValueToPointer()
    {
        UIRoot root = new(200, 100);
        Slider slider = new()
        {
            Minimum = 0,
            Maximum = 1,
            Value = 1
        };
        slider.Measure(new MeasureContext(new LayoutSize(110, 20)));
        slider.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));
        root.VisualChildren.Add(slider);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(30, 5, currentDown: true));

        Assert.Equal(0.25f, slider.Value);
    }

    [Fact]
    public void SliderOrientationAffectsTrackLayout()
    {
        Slider slider = new()
        {
            Orientation = Orientation.Vertical,
            Minimum = 0,
            Maximum = 100,
            Value = 50
        };

        slider.Measure(new MeasureContext(new LayoutSize(20, 110)));
        slider.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 110)));

        Assert.Equal(new LayoutRect(0, 50, 20, 10), slider.Track.Thumb.ArrangedBounds);
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

    private static InputFrame PointerFrame(float x, float y, bool currentDown = false)
    {
        return PointerFrame(x, y, x, y, currentDown: currentDown);
    }
}
