using System.Reflection;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public delegate void RenderSurface2DDrawEventHandler(
    RenderSurface2D sender,
    RenderSurface2DFrame frame);

public class RenderSurface2D : ContentControl,
    ITimeSensitiveRenderElement,
    IRenderSurface2DFrameSource
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
    private readonly Dictionary<object, IRenderSurface2DBackendState> backendStates =
        new(ReferenceEqualityComparer.Instance);
    private HashSet<IDrawImageInvalidationSource> imageDependencies =
        new(ReferenceEqualityComparer.Instance);
    private HashSet<IDrawImageInvalidationSource> pendingImageDependencies =
        new(ReferenceEqualityComparer.Instance);
    private RenderSurface2DDrawEventHandler? draw;
    private long frameVersion = 1;
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
        AdvanceFrameVersion();
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
            AdvanceFrameVersion();
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
            AdvanceFrameVersion();
        }
    }

    internal bool IsDrawingActiveForTests => IsDrawingActive;

    private bool IsDrawingActive => draw is not null || hasOnDrawOverride;

    Color IRenderSurface2DFrameSource.ClearColor => ClearColor;

    long IRenderSurface2DFrameSource.FrameVersion => frameVersion;

    void IRenderSurface2DFrameSource.RecordFrame(
        DrawCommandList commands,
        DrawRect bounds)
    {
        pendingImageDependencies.Clear();
        RenderSurface2DFrame frame = new(
            commands,
            bounds,
            currentFrameTime,
            TrackImageDependency);
        try
        {
            InvokeDraw(frame);
            frame.Complete();
            CommitImageDependencies();
        }
        catch
        {
            pendingImageDependencies.Clear();
            throw;
        }
    }

    IRenderSurface2DBackendState? IRenderSurface2DFrameSource.GetBackendState(
        object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return backendStates.GetValueOrDefault(owner);
    }

    void IRenderSurface2DFrameSource.SetBackendState(
        object owner,
        IRenderSurface2DBackendState? state)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (backendStates.Remove(owner, out IRenderSurface2DBackendState? previous) &&
            !ReferenceEquals(previous, state))
        {
            previous.Dispose();
        }

        if (state is not null)
        {
            backendStates[owner] = state;
        }
    }

    private void HandleDrawingMutation(bool wasDrawingActive)
    {
        bool isDrawingActive = IsDrawingActive;
        if (!isDrawingActive)
        {
            DisposeManagedSession();
        }

        InvalidateFrame();
    }

    private void InvokeDraw(RenderSurface2DFrame frame)
    {
        OnDraw(frame);

        if (draw is null)
        {
            return;
        }

        foreach (RenderSurface2DDrawEventHandler handler in draw.GetInvocationList())
        {
            handler(this, frame);
        }
    }

    private void TrackImageDependency(IDrawImage image)
    {
        if (image is IDrawImageInvalidationSource dependency)
        {
            pendingImageDependencies.Add(dependency);
        }
    }

    private void CommitImageDependencies()
    {
        foreach (IDrawImageInvalidationSource dependency in imageDependencies)
        {
            if (!pendingImageDependencies.Contains(dependency))
            {
                dependency.ContentChanged -= OnImageContentChanged;
            }
        }

        foreach (IDrawImageInvalidationSource dependency in pendingImageDependencies)
        {
            if (!imageDependencies.Contains(dependency))
            {
                dependency.ContentChanged += OnImageContentChanged;
            }
        }

        (imageDependencies, pendingImageDependencies) =
            (pendingImageDependencies, imageDependencies);
        pendingImageDependencies.Clear();
    }

    private void OnImageContentChanged(object? sender, EventArgs args)
    {
        if (RedrawMode == RenderSurface2DRedrawMode.OnDemand)
        {
            InvalidateFrame();
        }
    }

    private void AdvanceFrameVersion()
    {
        frameVersion = frameVersion == long.MaxValue
            ? 1
            : frameVersion + 1;
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

    private void DisposeManagedSession()
    {
        foreach (IDrawImageInvalidationSource dependency in imageDependencies)
        {
            dependency.ContentChanged -= OnImageContentChanged;
        }

        imageDependencies.Clear();
        pendingImageDependencies.Clear();
        foreach (IRenderSurface2DBackendState state in backendStates.Values)
        {
            state.Dispose();
        }

        backendStates.Clear();
    }
}
