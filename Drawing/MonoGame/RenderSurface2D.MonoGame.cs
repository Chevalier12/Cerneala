using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.UI.Controls;

public partial class RenderSurface2D : Cerneala.Drawing.MonoGame.IMonoGameRenderSurface2DSource
{
    private Cerneala.Drawing.MonoGame.MonoGameRenderSurface2DSession? managedSession;

    Texture2D? Cerneala.Drawing.MonoGame.IMonoGameRenderSurface2DSource.ResolveSurface(
        GraphicsDevice graphicsDevice,
        int pixelWidth,
        int pixelHeight)
    {
        EnsureManagedSession(graphicsDevice, pixelWidth, pixelHeight);
        Cerneala.Drawing.MonoGame.MonoGameRenderSurface2DSession session = managedSession!;
        if (managedSurfaceDirty)
        {
            session.Render(
                InvokeDraw,
                ClearColor,
                currentFrameTime);
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

    private partial void DisposeManagedSession()
    {
        managedSession?.Dispose();
        managedSession = null;
    }
}
