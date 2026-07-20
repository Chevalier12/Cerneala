using Cerneala.Drawing.Prism;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism;

public interface IMonoGameBackdropFrameLease : IBackdropFrameLease
{
    Texture2D Texture { get; }
}
