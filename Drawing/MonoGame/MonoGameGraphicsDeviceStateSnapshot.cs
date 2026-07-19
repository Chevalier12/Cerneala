using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame;

internal sealed class MonoGameGraphicsDeviceStateSnapshot
{
    private RenderTargetBinding[] renderTargets = [];
    private Viewport viewport;
    private Rectangle scissorRectangle;
    private BlendState? blendState;
    private Microsoft.Xna.Framework.Color blendFactor;
    private DepthStencilState? depthStencilState;
    private RasterizerState? rasterizerState;
    private SamplerState? samplerState;
    private Texture? texture;
    private IndexBuffer? indexBuffer;
    private bool captured;

    public void Capture(GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (captured)
        {
            throw new InvalidOperationException("The graphics-device state snapshot is already active.");
        }

        renderTargets = device.GetRenderTargets();
        viewport = device.Viewport;
        scissorRectangle = device.ScissorRectangle;
        blendState = device.BlendState;
        blendFactor = device.BlendFactor;
        depthStencilState = device.DepthStencilState;
        rasterizerState = device.RasterizerState;
        samplerState = device.SamplerStates[0];
        texture = device.Textures[0];
        indexBuffer = device.Indices;
        captured = true;
    }

    public void Restore(GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (!captured)
        {
            return;
        }

        try
        {
            RestoreRenderTargetsAndViewport(device);
            device.ScissorRectangle = scissorRectangle;
            device.BlendState = blendState!;
            device.BlendFactor = blendFactor;
            device.DepthStencilState = depthStencilState!;
            device.RasterizerState = rasterizerState!;
            device.SamplerStates[0] = samplerState!;
            device.Textures[0] = texture;
            device.Indices = indexBuffer;
        }
        finally
        {
            renderTargets = [];
            blendState = null;
            depthStencilState = null;
            rasterizerState = null;
            samplerState = null;
            texture = null;
            indexBuffer = null;
            captured = false;
        }
    }

    public void RestoreRenderTargetsAndViewport(GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (!captured)
        {
            throw new InvalidOperationException(
                "The graphics-device state snapshot is not active.");
        }

        if (renderTargets.Length == 0)
        {
            device.SetRenderTarget(null);
        }
        else
        {
            device.SetRenderTargets(renderTargets);
        }

        device.Viewport = viewport;
    }
}
