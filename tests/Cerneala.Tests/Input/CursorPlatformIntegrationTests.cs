using System.Reflection;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Platform;

namespace Cerneala.Tests.Input;

public sealed class CursorPlatformIntegrationTests
{
    private static readonly UiViewport Viewport = new(100, 100);

    [Fact]
    public void HoveringButtonPublishesHandCursorToPlatformService()
    {
        FakeCursorService cursor = new();
        UiHost host = HostWithSingleChild(new Button { Content = new FixedSizeElement(40, 24) }, cursor);

        host.Update(PointerFrame(10, 10), Viewport, TimeSpan.Zero);

        Assert.Equal(CursorShape.Hand, cursor.Current);
        Assert.Equal([CursorShape.Hand], cursor.Published);
    }

    [Fact]
    public void HoveringTextBoxPublishesIBeamCursorToPlatformService()
    {
        FakeCursorService cursor = new();
        UiHost host = HostWithSingleChild(new FixedSizeTextBox(), cursor);

        host.Update(PointerFrame(10, 10), Viewport, TimeSpan.Zero);

        Assert.Equal(CursorShape.IBeam, cursor.Current);
        Assert.Equal([CursorShape.IBeam], cursor.Published);
    }

    [Fact]
    public void HoveringEmptyRootPublishesArrowCursor()
    {
        FakeCursorService cursor = new();
        UiHost host = HostWithRoot(new UIRoot(), cursor);

        host.Update(PointerFrame(10, 10), Viewport, TimeSpan.Zero);

        Assert.Equal(CursorShape.Arrow, cursor.Current);
        Assert.Equal([CursorShape.Arrow], cursor.Published);
    }

    [Fact]
    public void HiddenElementDoesNotPublishItsCursor()
    {
        FakeCursorService cursor = new();
        Button hiddenButton = new()
        {
            Content = new FixedSizeElement(40, 24),
            IsVisible = false
        };
        UiHost host = HostWithSingleChild(hiddenButton, cursor);

        host.Update(PointerFrame(10, 10), Viewport, TimeSpan.Zero);

        Assert.Equal(CursorShape.Arrow, cursor.Current);
        Assert.DoesNotContain(CursorShape.Hand, cursor.Published);
    }

    [Fact]
    public void CursorResolutionUsesRetainedInputCacheWithoutRebuildOnUnchangedFrame()
    {
        FakeCursorService cursor = new();
        UiHost host = HostWithSingleChild(new Button { Content = new FixedSizeElement(40, 24) }, cursor, out UIRoot root);
        host.Update(PointerFrame(10, 10), Viewport, TimeSpan.Zero);
        int rebuildsAfterInitialFrame = root.InputCache.RebuildCount;

        host.Update(PointerFrame(10, 10), Viewport, TimeSpan.Zero);

        Assert.Equal(rebuildsAfterInitialFrame, root.InputCache.RebuildCount);
        Assert.Equal(CursorShape.Hand, cursor.Current);
        Assert.Equal(CursorShape.Hand, cursor.Published.Last());
    }

    private static UiHost HostWithSingleChild(UIElement child, FakeCursorService cursor)
    {
        return HostWithSingleChild(child, cursor, out _);
    }

    private static UiHost HostWithSingleChild(UIElement child, FakeCursorService cursor, out UIRoot root)
    {
        root = new UIRoot();
        root.VisualChildren.Add(child);

        return HostWithRoot(root, cursor);
    }

    private static UiHost HostWithRoot(UIRoot root, FakeCursorService cursor)
    {
        UiHostOptions options = new() { Root = root };
        SetOptionsPlatformServices(options, new FakePlatformServices(cursor));

        return new UiHost(options);
    }

    private static InputFrame PointerFrame(float x, float y)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static void SetOptionsPlatformServices(UiHostOptions options, IPlatformServices? services)
    {
        PropertyInfo property = RequiredProperty(typeof(UiHostOptions), "PlatformServices");
        Assert.True(property.SetMethod is not null, "UiHostOptions.PlatformServices must be settable by host options.");
        property.SetValue(options, services);
    }

    private static PropertyInfo RequiredProperty(Type type, string name)
    {
        PropertyInfo? property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        Assert.True(property is not null, $"{type.Name}.{name} is required before cursor publishing can be wired.");
        return property!;
    }

    private sealed class FakePlatformServices(ICursorService cursor) : IPlatformServices
    {
        public IClipboard? Clipboard => null;

        public ICursorService? Cursor { get; } = cursor;

        public IFileDialogService? FileDialogs => null;

        public ITextInputPlatform? TextInput => null;

        public IDpiProvider? Dpi => null;

        public IAccessibilityPlatform? Accessibility => null;

        public IReducedMotionSource? ReducedMotion => null;
    }

    private sealed class FakeCursorService : ICursorService
    {
        private readonly List<CursorShape> published = [];

        public CursorShape Current { get; private set; }

        public IReadOnlyList<CursorShape> Published => published;

        public void SetCursor(CursorShape shape)
        {
            Current = shape;
            published.Add(shape);
        }
    }

    private sealed class FixedSizeTextBox : TextBox
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(60, 24);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            return context.FinalRect;
        }
    }

    private sealed class FixedSizeElement(float width, float height) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(width, height);
        }
    }
}
