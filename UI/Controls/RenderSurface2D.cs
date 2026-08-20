using System.Reflection;
using Cerneala.Drawing;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public delegate void RenderSurface2DDrawEventHandler(
    RenderSurface2DDrawContext context);

public partial class RenderSurface2D : ContentControl,
    ITimeSensitiveRenderElement,
    IRenderSurface2DSource
{
    private static readonly object DrawOverrideCacheLock = new();
    private static readonly Dictionary<Type, bool> DrawOverrideCache = [];

    private readonly bool hasDrawSurfaceOverride;
    private RenderSurface2DDrawEventHandler? drawSurface;
    private bool managedSurfaceDirty = true;

    public RenderSurface2D()
    {
        hasDrawSurfaceOverride = DetectDrawSurfaceOverride(GetType());
    }

    public event RenderSurface2DDrawEventHandler? DrawSurface
    {
        add
        {
            bool wasManagedModeActive = IsManagedModeActive;
            drawSurface += value;
            HandleManagedModeMutation(wasManagedModeActive);
        }
        remove
        {
            bool wasManagedModeActive = IsManagedModeActive;
            drawSurface -= value;
            HandleManagedModeMutation(wasManagedModeActive);
        }
    }

    public void RefreshSurface()
    {
        if (IsManagedModeActive)
        {
            managedSurfaceDirty = true;
        }

        IncrementRenderVersion();
        Invalidate(InvalidationFlags.Render, "RenderSurface2D content changed");
    }

    public bool UpdateRenderTime(TimeSpan frameTime)
    {
        _ = frameTime;
        if (!IsManagedModeActive)
        {
            return false;
        }

        RefreshSurface();
        return true;
    }

    protected virtual void OnDrawSurface(
        RenderSurface2DDrawContext context)
    {
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (IsManagedModeActive)
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
            (IsManagedModeActive || HasPresentedSurface()))
        {
            context.DrawingContext.DrawRenderSurface2D(
                this,
                bounds,
                Cerneala.Drawing.Color.White);
        }

        Border.RenderBorder(this, context);
    }

    internal bool IsManagedModeActiveForTests => IsManagedModeActive;

    private bool IsManagedModeActive =>
        drawSurface is not null || hasDrawSurfaceOverride;

    private void HandleManagedModeMutation(bool wasManagedModeActive)
    {
        bool isManagedModeActive = IsManagedModeActive;
        if (!isManagedModeActive)
        {
            DisposeManagedSession();
        }
        else if (!wasManagedModeActive)
        {
            managedSurfaceDirty = true;
        }

        RefreshSurface();
    }

    private static bool DetectDrawSurfaceOverride(Type type)
    {
        lock (DrawOverrideCacheLock)
        {
            if (DrawOverrideCache.TryGetValue(type, out bool cached))
            {
                return cached;
            }

            MethodInfo? method = type.GetMethod(
                nameof(OnDrawSurface),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                [typeof(RenderSurface2DDrawContext)],
                modifiers: null);
            bool hasOverride = method?.GetBaseDefinition().DeclaringType !=
                method?.DeclaringType;
            DrawOverrideCache[type] = hasOverride;
            return hasOverride;
        }
    }

    private partial bool HasPresentedSurface();

    private partial void DisposeManagedSession();
}
