using System.Reflection;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Shaders;

internal enum PrismShaderId
{
    CopyComposite,
    Styles,
    Charcoal,
    ConteCrayon,
    GraphicPen,
    Plaster,
    Deinterlace
}

internal static class PrismShaderResources
{
    private const string ResourcePrefix = "Cerneala.Drawing.MonoGame.Prism.Shaders.";
    private static readonly Lazy<byte[]> CopyCompositeBytecode =
        new(() => LoadEmbeddedBytecode("CopyComposite.mgfxo"));
    private static readonly Lazy<byte[]> StylesBytecode =
        new(() => LoadEmbeddedBytecode("Styles.mgfxo"));
    private static readonly Lazy<byte[]> CharcoalBytecode =
        new(() => LoadEmbeddedBytecode("Charcoal.mgfxo"));
    private static readonly Lazy<byte[]> ConteCrayonBytecode =
        new(() => LoadEmbeddedBytecode("ConteCrayon.mgfxo"));
    private static readonly Lazy<byte[]> GraphicPenBytecode =
        new(() => LoadEmbeddedBytecode("GraphicPen.mgfxo"));
    private static readonly Lazy<byte[]> PlasterBytecode =
        new(() => LoadEmbeddedBytecode("Plaster.mgfxo"));
    private static readonly Lazy<byte[]> DeinterlaceBytecode =
        new(() => LoadEmbeddedBytecode("Deinterlace.mgfxo"));

    internal static ReadOnlyMemory<byte> GetBytecode(PrismShaderId shader)
    {
        return GetBytecodeArray(shader);
    }

    internal static Effect CreateEffect(GraphicsDevice graphicsDevice, PrismShaderId shader)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        return new Effect(graphicsDevice, GetBytecodeArray(shader));
    }

    private static byte[] GetBytecodeArray(PrismShaderId shader)
    {
        return shader switch
        {
            PrismShaderId.CopyComposite => CopyCompositeBytecode.Value,
            PrismShaderId.Styles => StylesBytecode.Value,
            PrismShaderId.Charcoal => CharcoalBytecode.Value,
            PrismShaderId.ConteCrayon => ConteCrayonBytecode.Value,
            PrismShaderId.GraphicPen => GraphicPenBytecode.Value,
            PrismShaderId.Plaster => PlasterBytecode.Value,
            PrismShaderId.Deinterlace => DeinterlaceBytecode.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(shader), shader, "Unknown Prism shader.")
        };
    }

    private static byte[] LoadEmbeddedBytecode(string fileName)
    {
        Assembly assembly = typeof(PrismShaderResources).Assembly;
        string resourceName = ResourcePrefix + fileName;
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded Prism shader '{resourceName}' is missing. Run a clean build to recompile shaders.");
        using MemoryStream buffer = new(checked((int)stream.Length));
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
