using System.Reflection;
using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Platform;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Platform;

public sealed class UiHostPlatformServicesIntegrationTests
{
    private static readonly UiViewport Viewport = new(100, 100);

    [Fact]
    public void UiHostOptionsAttachPlatformServicesToRoot()
    {
        UIRoot root = new();
        FakePlatformServices services = new();
        UiHostOptions options = new() { Root = root };
        SetOptionsPlatformServices(options, services);

        UiHost host = new(options);

        Assert.Same(root, host.Root);
        Assert.Same(services, GetRootPlatformServices(root));
    }

    [Fact]
    public void SetRootReattachesPlatformServicesToNewRoot()
    {
        UIRoot firstRoot = new();
        UIRoot nextRoot = new();
        FakePlatformServices services = new();
        UiHostOptions options = new() { Root = firstRoot };
        SetOptionsPlatformServices(options, services);
        UiHost host = new(options);

        host.SetRoot(nextRoot);

        Assert.Same(nextRoot, host.Root);
        Assert.Same(services, GetRootPlatformServices(nextRoot));
    }

    [Fact]
    public void ReplacingPlatformServicesDoesNotInvalidateLayoutOrRender()
    {
        UiHost host = HostWithRenderableRoot(out UIRoot root, out RenderCountingElement child);
        host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);
        int renderCountAfterFirstFrame = child.RenderCount;
        FakePlatformServices replacement = new();

        SetRootPlatformServices(root, replacement);
        UiFrame frame = host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);

        Assert.Same(replacement, GetRootPlatformServices(root));
        Assert.Equal(0, frame.Stats.MeasuredElements);
        Assert.Equal(0, frame.Stats.ArrangedElements);
        Assert.Equal(0, frame.Stats.RenderedElements);
        Assert.Equal(renderCountAfterFirstFrame, child.RenderCount);
    }

    [Fact]
    public void PlatformServicesCanBeNullAndUpdateStillWorks()
    {
        UIRoot root = new();
        UiHostOptions options = new() { Root = root };
        SetOptionsPlatformServices(options, null);
        UiHost host = new(options);

        UiFrame frame = host.Update(EmptyFrame(), Viewport, TimeSpan.Zero);

        Assert.Same(PlatformServices.Empty, GetRootPlatformServices(root));
        Assert.True(frame.Stats.HasWork);
    }

    private static UiHost HostWithRenderableRoot(out UIRoot root, out RenderCountingElement child)
    {
        root = new UIRoot();
        child = new RenderCountingElement();
        root.VisualChildren.Add(child);

        return new UiHost(new UiHostOptions { Root = root });
    }

    private static InputFrame EmptyFrame()
    {
        return new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static void SetOptionsPlatformServices(UiHostOptions options, IPlatformServices? services)
    {
        PropertyInfo property = RequiredProperty(typeof(UiHostOptions), "PlatformServices");
        Assert.True(property.SetMethod is not null, "UiHostOptions.PlatformServices must be settable by host options.");
        property.SetValue(options, services);
    }

    private static IPlatformServices GetRootPlatformServices(UIRoot root)
    {
        PropertyInfo property = RequiredProperty(typeof(UIRoot), "PlatformServices");
        object? value = property.GetValue(root);

        IPlatformServices services = Assert.IsAssignableFrom<IPlatformServices>(value);
        return services;
    }

    private static void SetRootPlatformServices(UIRoot root, IPlatformServices? services)
    {
        MethodInfo method = RequiredMethod(typeof(UIRoot), "SetPlatformServices");
        method.Invoke(root, [services]);
    }

    private static PropertyInfo RequiredProperty(Type type, string name)
    {
        PropertyInfo? property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        Assert.True(property is not null, $"{type.Name}.{name} is required for platform service ownership.");
        return property!;
    }

    private static MethodInfo RequiredMethod(Type type, string name)
    {
        MethodInfo? method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
        Assert.True(method is not null, $"{type.Name}.{name} is required for platform service ownership.");
        return method!;
    }

    private sealed class FakePlatformServices : IPlatformServices
    {
        public IClipboard? Clipboard => null;

        public ICursorService? Cursor => null;

        public IFileDialogService? FileDialogs => null;

        public ITextInputPlatform? TextInput => null;

        public IDpiProvider? Dpi => null;

        public IAccessibilityPlatform? Accessibility => null;

        public IReducedMotionSource? ReducedMotion => null;
    }

    private sealed class RenderCountingElement : UIElement
    {
        public int RenderCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(10, 10);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            return new LayoutRect(context.FinalRect.X, context.FinalRect.Y, DesiredSize.Width, DesiredSize.Height);
        }

        protected override void OnRender(RenderContext context)
        {
            RenderCount++;
            context.DrawingContext.FillRectangle(new DrawRect(context.Bounds.X, context.Bounds.Y, 1, 1), DrawColor.White);
        }
    }
}
