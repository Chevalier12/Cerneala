using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.UI.Controls;

public partial class RenderSurface2D : Cerneala.Drawing.MonoGame.IMonoGameRenderSurface2DSource
{
    private Cerneala.Drawing.MonoGame.MonoGameRenderSurface2DSession? managedSession;
    private HashSet<Cerneala.Drawing.IDrawImageInvalidationSource> imageDependencies =
        new(ReferenceEqualityComparer.Instance);
    private HashSet<Cerneala.Drawing.IDrawImageInvalidationSource> pendingImageDependencies =
        new(ReferenceEqualityComparer.Instance);

    Texture2D? Cerneala.Drawing.MonoGame.IMonoGameRenderSurface2DSource.ResolveSurface(
        GraphicsDevice graphicsDevice,
        int pixelWidth,
        int pixelHeight)
    {
        EnsureManagedSession(graphicsDevice, pixelWidth, pixelHeight);
        Cerneala.Drawing.MonoGame.MonoGameRenderSurface2DSession session = managedSession!;
        if (managedSurfaceDirty)
        {
            pendingImageDependencies.Clear();
            try
            {
                session.Render(
                    InvokeDraw,
                    ClearColor,
                    currentFrameTime,
                    TrackImageDependency);
                CommitImageDependencies();
                managedSurfaceDirty = false;
            }
            catch
            {
                pendingImageDependencies.Clear();
                throw;
            }
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

    private void TrackImageDependency(Cerneala.Drawing.IDrawImage image)
    {
        if (image is Cerneala.Drawing.IDrawImageInvalidationSource dependency)
        {
            pendingImageDependencies.Add(dependency);
        }
    }

    private void CommitImageDependencies()
    {
        foreach (Cerneala.Drawing.IDrawImageInvalidationSource dependency in imageDependencies)
        {
            if (!pendingImageDependencies.Contains(dependency))
            {
                dependency.ContentChanged -= OnImageContentChanged;
            }
        }

        foreach (Cerneala.Drawing.IDrawImageInvalidationSource dependency in pendingImageDependencies)
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
        if (RedrawMode == RenderSurface2DRedrawMode.OnDemand &&
            !managedSurfaceDirty)
        {
            InvalidateFrame();
        }
    }

    private partial void DisposeManagedSession()
    {
        foreach (Cerneala.Drawing.IDrawImageInvalidationSource dependency in imageDependencies)
        {
            dependency.ContentChanged -= OnImageContentChanged;
        }

        imageDependencies.Clear();
        pendingImageDependencies.Clear();
        managedSession?.Dispose();
        managedSession = null;
    }
}
