using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.UI.Controls;

public sealed class RenderSurface2DDrawContext
{
    private bool batchActive;

    internal RenderSurface2DDrawContext(
        SpriteBatch spriteBatch,
        Rectangle bounds)
    {
        SpriteBatch = spriteBatch ??
            throw new ArgumentNullException(nameof(spriteBatch));
        Bounds = bounds;
    }

    public SpriteBatch SpriteBatch { get; }

    public GraphicsDevice GraphicsDevice => SpriteBatch.GraphicsDevice;

    public Rectangle Bounds { get; }

    public bool IsBatchActive => batchActive;

    public void Begin(
        SpriteSortMode sortMode = SpriteSortMode.Deferred,
        BlendState? blendState = null,
        SamplerState? samplerState = null,
        DepthStencilState? depthStencilState = null,
        RasterizerState? rasterizerState = null,
        Effect? effect = null,
        Matrix? transformMatrix = null)
    {
        if (batchActive)
        {
            throw new InvalidOperationException(
                "The RenderSurface2D SpriteBatch is already active.");
        }

        SpriteBatch.Begin(
            sortMode,
            blendState ?? BlendState.AlphaBlend,
            samplerState ?? SamplerState.LinearClamp,
            depthStencilState ?? DepthStencilState.None,
            rasterizerState ?? RasterizerState.CullNone,
            effect,
            transformMatrix);
        batchActive = true;
    }

    public void End()
    {
        if (!batchActive)
        {
            throw new InvalidOperationException(
                "The RenderSurface2D SpriteBatch is not active.");
        }

        try
        {
            SpriteBatch.End();
        }
        finally
        {
            batchActive = false;
        }
    }

    internal void CompleteBatch()
    {
        if (batchActive)
        {
            End();
        }
    }
}
