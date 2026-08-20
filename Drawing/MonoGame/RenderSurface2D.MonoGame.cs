using Cerneala.UI.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.UI.Controls;

public partial class RenderSurface2D : Cerneala.Drawing.MonoGame.IMonoGameRenderSurface2DSource
{
    private Cerneala.Drawing.MonoGame.MonoGameRenderSurface2DSession? managedSession;

    public static readonly UiProperty<Texture2D?> SurfaceProperty =
        UiProperty<Texture2D?>.Register(
            nameof(Surface),
            typeof(RenderSurface2D),
            new UiPropertyMetadata<Texture2D?>(
                null,
                UiPropertyOptions.AffectsRender,
                ReferenceTextureComparer.Instance));

    public Texture2D? Surface
    {
        get => GetValue(SurfaceProperty);
        set => SetValue(SurfaceProperty, value);
    }

    public void Present(Texture2D? surface)
    {
        if (ReferenceEquals(Surface, surface))
        {
            if (surface is not null)
            {
                RefreshSurface();
            }

            return;
        }

        Surface = surface;
    }

    public void ClearSurface()
    {
        Surface = null;
    }

    Texture2D? Cerneala.Drawing.MonoGame.IMonoGameRenderSurface2DSource.ResolveSurface(
        GraphicsDevice graphicsDevice,
        int pixelWidth,
        int pixelHeight)
    {
        if (!IsManagedModeActive)
        {
            DisposeManagedSession();
            return Surface;
        }

        EnsureManagedSession(graphicsDevice, pixelWidth, pixelHeight);
        Cerneala.Drawing.MonoGame.MonoGameRenderSurface2DSession session = managedSession!;
        if (managedSurfaceDirty)
        {
            session.Render(InvokeManagedDraw);
            managedSurfaceDirty = false;
        }

        return session.Surface;
    }

    private void EnsureManagedSession(
        GraphicsDevice graphicsDevice,
        int pixelWidth,
        int pixelHeight)
    {
        if (managedSession is not null &&
            !managedSession.IsDisposed &&
            ReferenceEquals(managedSession.GraphicsDevice, graphicsDevice) &&
            managedSession.PixelWidth == pixelWidth &&
            managedSession.PixelHeight == pixelHeight)
        {
            return;
        }

        DisposeManagedSession();
        managedSession = new Cerneala.Drawing.MonoGame.MonoGameRenderSurface2DSession(
            graphicsDevice,
            pixelWidth,
            pixelHeight);
        managedSurfaceDirty = true;
    }

    private void InvokeManagedDraw(SpriteBatch spriteBatch, Rectangle bounds)
    {
        RenderSurface2DDrawContext context = new(spriteBatch, bounds);
        try
        {
            OnDrawSurface(context);
        }
        finally
        {
            context.CompleteBatch();
        }

        if (drawSurface is null)
        {
            return;
        }

        foreach (RenderSurface2DDrawEventHandler handler in drawSurface.GetInvocationList())
        {
            try
            {
                handler(context);
            }
            finally
            {
                context.CompleteBatch();
            }
        }
    }

    private partial bool HasPresentedSurface() => Surface is not null;

    private partial void DisposeManagedSession()
    {
        managedSession?.Dispose();
        managedSession = null;
    }

    private sealed class ReferenceTextureComparer : IEqualityComparer<Texture2D?>
    {
        public static readonly ReferenceTextureComparer Instance = new();

        public bool Equals(Texture2D? x, Texture2D? y) => ReferenceEquals(x, y);

        public int GetHashCode(Texture2D? obj) => obj is null
            ? 0
            : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
