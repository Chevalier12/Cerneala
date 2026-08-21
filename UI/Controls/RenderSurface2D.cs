using System.Reflection;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public delegate void RenderSurface2DDrawEventHandler(
    RenderSurface2D sender,
    RenderSurface2DFrame frame);

public partial class RenderSurface2D : ContentControl,
    ITimeSensitiveRenderElement,
    IRenderSurface2DSource
{
    public static readonly UiProperty<Color> ClearColorProperty =
        UiProperty<Color>.Register(
            nameof(ClearColor),
            typeof(RenderSurface2D),
            new UiPropertyMetadata<Color>(
                Color.Transparent,
                UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<RenderSurface2DRedrawMode> RedrawModeProperty =
        UiProperty<RenderSurface2DRedrawMode>.Register(
            nameof(RedrawMode),
            typeof(RenderSurface2D),
            new UiPropertyMetadata<RenderSurface2DRedrawMode>(
                RenderSurface2DRedrawMode.Continuous,
                UiPropertyOptions.AffectsRender));

    private static readonly object OnDrawOverrideCacheLock = new();
    private static readonly Dictionary<Type, bool> OnDrawOverrideCache = [];

    private readonly bool hasOnDrawOverride;
    private RenderSurface2DDrawEventHandler? draw;
    private bool managedSurfaceDirty = true;
    private TimeSpan currentFrameTime;

    public RenderSurface2D()
    {
        hasOnDrawOverride = DetectOnDrawOverride(GetType());
    }

    public Color ClearColor
    {
        get => GetValue(ClearColorProperty);
        set => SetValue(ClearColorProperty, value);
    }

    public RenderSurface2DRedrawMode RedrawMode
    {
        get => GetValue(RedrawModeProperty);
        set => SetValue(RedrawModeProperty, value);
    }

    public event RenderSurface2DDrawEventHandler? Draw
    {
        add
        {
            bool wasDrawingActive = IsDrawingActive;
            draw += value;
            HandleDrawingMutation(wasDrawingActive);
        }
        remove
        {
            bool wasDrawingActive = IsDrawingActive;
            draw -= value;
            HandleDrawingMutation(wasDrawingActive);
        }
    }

    public void InvalidateFrame()
    {
        managedSurfaceDirty = true;
        IncrementRenderVersion();
        Invalidate(InvalidationFlags.Render, "RenderSurface2D frame changed");
    }

    bool ITimeSensitiveRenderElement.UpdateRenderTime(TimeSpan frameTime)
    {
        currentFrameTime = frameTime;
        if (!IsDrawingActive || RedrawMode != RenderSurface2DRedrawMode.Continuous)
        {
            return false;
        }

        InvalidateFrame();
        return true;
    }

    protected virtual void OnDraw(RenderSurface2DFrame frame)
    {
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (IsDrawingActive)
        {
            managedSurfaceDirty = true;
        }
    }

    protected override void OnDetached()
    {
        DisposeManagedSession();
        base.OnDetached();
    }

    protected override void OnRender(RenderContext context)
    {
        Border.RenderBackground(this, context);
        DrawRect bounds = Border.ToDrawRect(context.Bounds);
        if (bounds.Width > 0 &&
            bounds.Height > 0 &&
            IsDrawingActive)
        {
            context.DrawingContext.DrawRenderSurface2D(
                this,
                bounds,
                Color.White);
        }

        Border.RenderBorder(this, context);
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ClearColorProperty) ||
            ReferenceEquals(args.Property, RedrawModeProperty))
        {
            managedSurfaceDirty = true;
        }
    }

    internal bool IsDrawingActiveForTests => IsDrawingActive;

    private bool IsDrawingActive => draw is not null || hasOnDrawOverride;

    private void HandleDrawingMutation(bool wasDrawingActive)
    {
        bool isDrawingActive = IsDrawingActive;
        if (!isDrawingActive)
        {
            DisposeManagedSession();
        }
        else if (!wasDrawingActive)
        {
            managedSurfaceDirty = true;
        }

        InvalidateFrame();
    }

    private static bool DetectOnDrawOverride(Type type)
    {
        lock (OnDrawOverrideCacheLock)
        {
            if (OnDrawOverrideCache.TryGetValue(type, out bool cached))
            {
                return cached;
            }

            MethodInfo? method = type.GetMethod(
                nameof(OnDraw),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                [typeof(RenderSurface2DFrame)],
                modifiers: null);
            bool hasOverride = method?.GetBaseDefinition().DeclaringType !=
                method?.DeclaringType;
            OnDrawOverrideCache[type] = hasOverride;
            return hasOverride;
        }
    }

    private partial void DisposeManagedSession();
}
