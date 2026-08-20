using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Execution;

internal interface IPrismCommandRenderer
{
    GraphicsDevice GraphicsDevice { get; }

    void BeginCommandBatch();

    void BeginKernelBatch(
        Effect effect,
        BlendState blendState,
        SamplerState samplerState,
        Rectangle? scissorRectangle = null);

    void EndBatch();

    void RenderCommand(DrawCommand command);

    void DrawFullscreen(
        Texture2D texture,
        Rectangle destination);

    void RestoreHostTarget();
}
