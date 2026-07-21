using System.Runtime.CompilerServices;
using Cerneala.Presentation;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Core;
using Cerneala.Tests.UI.Motion.Core;

namespace Cerneala.Tests.Presentation;

public sealed class SolarSystemChapterViewTests
{
    [Fact]
    public void VisibilityLifecycle_StartsAndCancelsForeverMotion()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(1000, 700, motionClock: clock);
        SolarSystemChapterView view = new() { Visibility = Visibility.Collapsed };
        root.VisualChildren.Add(view);
        root.ProcessFrame();

        Assert.False(root.Motion.HasActiveMotion);

        view.Visibility = Visibility.Visible;
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(16));
        root.ProcessFrame();

        Assert.True(root.Motion.HasActiveMotion);

        view.Visibility = Visibility.Collapsed;
        root.ProcessFrame();

        Assert.False(root.Motion.HasActiveMotion);
        Assert.Equal(0, root.Motion.Graph.ActiveNodeCount);
    }

    [Fact]
    public void VisibleSolarSystem_AdvancesOrbitTransformsBetweenFrames()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(1000, 700, motionClock: clock);
        SolarSystemChapterView view = new() { Visibility = Visibility.Collapsed };
        root.VisualChildren.Add(view);
        root.ProcessFrame();

        view.Visibility = Visibility.Visible;
        root.ProcessFrame();
        UIElement mercuryBody = Assert.Single(
            Descendants(view).Where(element =>
                element.VisualChildren
                    .OfType<Cerneala.UI.Controls.TextBlock>()
                    .Any(label => label.Text == "Mercur")));
        UIElement mercuryOrbit = Assert.IsAssignableFrom<UIElement>(mercuryBody.VisualParent);
        float initialOrbitRotation = mercuryOrbit.Rotation;
        float initialBodyRotation = mercuryBody.Rotation;

        clock.Advance(TimeSpan.FromSeconds(1));
        root.ProcessFrame();

        Assert.NotEqual(initialOrbitRotation, mercuryOrbit.Rotation);
        Assert.NotEqual(initialBodyRotation, mercuryBody.Rotation);
    }

    [Fact]
    public void DetachedAnimatedView_IsCollectibleWhileRootRemainsAlive()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(1000, 700, motionClock: clock);
        WeakReference viewReference = AttachRunAndDetachView(root, clock);

        Assert.False(root.Motion.HasActiveMotion);
        Assert.Equal(0, root.Motion.Graph.ActiveNodeCount);
        Assert.Equal(0, root.Motion.Properties.BindingCount);

        for (int attempt = 0; attempt < 3 && viewReference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        Assert.False(viewReference.IsAlive);
        GC.KeepAlive(root);
    }

    [Theory]
    [InlineData("Mercur", "Planeta terestra", "57,9 mil. km", "4.879 km", "88 zile")]
    [InlineData("Venus", "Planeta terestra", "108,2 mil. km", "12.104 km", "225 zile")]
    [InlineData("Pamant", "Planeta terestra", "149,6 mil. km", "12.742 km", "365 zile")]
    [InlineData("Marte", "Planeta terestra", "227,9 mil. km", "6.779 km", "687 zile")]
    [InlineData("Jupiter", "Gigant gazos", "778,5 mil. km", "139.820 km", "11,9 ani")]
    [InlineData("Saturn", "Gigant gazos", "1,43 mld. km", "116.460 km", "29,5 ani")]
    [InlineData("Uranus", "Gigant de gheata", "2,87 mld. km", "50.724 km", "84 ani")]
    [InlineData("Neptun", "Gigant de gheata", "4,50 mld. km", "49.244 km", "164,8 ani")]
    public void PlanetClick_UpdatesInformationCard(
        string name,
        string type,
        string distance,
        string diameter,
        string orbitalYear)
    {
        SolarSystemChapterView view = new();
        UIRoot root = new(1000, 700);
        root.VisualChildren.Add(view);
        UIElement planet = Assert.Single(
            Descendants(view).Where(
                element =>
                    element.Width <= 54 &&
                    element.Height <= 30 &&
                    element.VisualChildren
                        .OfType<Cerneala.UI.Controls.TextBlock>()
                        .Any(label => label.Text == name)));
        view.Measure(new MeasureContext(new LayoutSize(1000, 700)));
        view.Arrange(new ArrangeContext(new LayoutRect(0, 0, 1000, 700)));
        UIElement orbit = Assert.IsAssignableFrom<UIElement>(planet.VisualParent);
        orbit.Rotation = 1.2f;
        LayoutPoint clickPosition = GetVisualCenter(view, planet);
        ElementInputBridge bridge = new();
        InputFrame press = PointerFrame(clickPosition, isDown: true);
        InputFrame release = PointerFrame(clickPosition, wasDown: true);

        bridge.Dispatch(root, press);
        bridge.Dispatch(root, release);

        string[] cardValues = Descendants(view)
            .OfType<Cerneala.UI.Controls.TextBlock>()
            .Select(text => text.Text)
            .ToArray();
        Assert.Contains(name, cardValues);
        Assert.Contains(type, cardValues);
        Assert.Contains(distance, cardValues);
        Assert.Contains(diameter, cardValues);
        Assert.Contains(orbitalYear, cardValues);
    }

    private static LayoutPoint GetVisualCenter(UIElement view, UIElement element)
    {
        LayoutPoint point = new(
            element.ArrangedBounds.X + (element.ArrangedBounds.Width / 2),
            element.ArrangedBounds.Y + (element.ArrangedBounds.Height / 2));

        for (UIElement? current = element; current is not null && current != view; current = current.VisualParent)
        {
            float pivotX = current.ArrangedBounds.X +
                (current.ArrangedBounds.Width * current.RenderTransformOrigin.X);
            float pivotY = current.ArrangedBounds.Y +
                (current.ArrangedBounds.Height * current.RenderTransformOrigin.Y);
            float offsetX = (point.X - pivotX) * current.Scale * current.ScaleX * current.PresenceScale;
            float offsetY = (point.Y - pivotY) * current.Scale * current.ScaleY * current.PresenceScale;
            float cosine = MathF.Cos(current.Rotation);
            float sine = MathF.Sin(current.Rotation);
            point = new LayoutPoint(
                pivotX + (offsetX * cosine) - (offsetY * sine) + current.TranslateX,
                pivotY + (offsetX * sine) + (offsetY * cosine) + current.TranslateY);
        }

        return point;
    }

    [Fact]
    public void MouseWheel_ZoomsSceneAndClampsScale()
    {
        SolarSystemChapterView view = new();

        MouseWheelEventArgs zoomIn = RaiseMouseWheel(view, 120);

        Assert.True(zoomIn.Handled);
        UIElement scene = Assert.Single(Descendants(view).Where(element => element.ScaleX > 1));
        Assert.Equal(scene.ScaleX, scene.ScaleY);

        for (int index = 0; index < 50; index++)
        {
            RaiseMouseWheel(view, 120);
        }

        Assert.Equal(3.4f, scene.ScaleX, 3);
        Assert.Equal(3.4f, scene.ScaleY, 3);

        for (int index = 0; index < 100; index++)
        {
            RaiseMouseWheel(view, -120);
        }

        Assert.Equal(0.7f, scene.ScaleX, 3);
        Assert.Equal(0.7f, scene.ScaleY, 3);
    }

    private static MouseWheelEventArgs RaiseMouseWheel(UIElement target, int delta)
    {
        MouseWheelEventArgs args = new(UIElement.MouseWheelEvent, target, 0, 0, delta);
        target.RaiseEvent(args);
        return args;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AttachRunAndDetachView(UIRoot root, ManualMotionClock clock)
    {
        SolarSystemChapterView view = new();
        root.VisualChildren.Add(view);
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(16));
        root.ProcessFrame();
        WeakReference reference = new(view);

        Assert.True(root.VisualChildren.Remove(view));
        root.ProcessFrame();
        return reference;
    }

    private static InputFrame PointerFrame(LayoutPoint position, bool wasDown = false, bool isDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(position.X, position.Y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(position.X, position.Y);
        if (wasDown)
        {
            previous = previous.WithButton(InputMouseButton.Left, true);
        }

        if (isDown)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static IEnumerable<UIElement> Descendants(UIElement parent)
    {
        foreach (UIElement child in parent.VisualChildren)
        {
            yield return child;

            foreach (UIElement descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
