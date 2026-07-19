using Cerneala.Drawing.MonoGame.Prism.Shaders;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismShaderResourcesTests
{
    [Fact]
    public void EveryKnownShaderHasEmbeddedBytecode()
    {
        foreach (PrismShaderId shader in Enum.GetValues<PrismShaderId>())
        {
            ReadOnlyMemory<byte> bytecode = PrismShaderResources.GetBytecode(shader);

            Assert.True(bytecode.Length > 32, $"Shader '{shader}' did not contain compiled MGFX bytecode.");
        }
    }

    [Fact]
    public void CopyCompositeBytecodeIsLoadedFromTheCernealaAssembly()
    {
        string[] resources = typeof(PrismShaderResources).Assembly.GetManifestResourceNames();

        Assert.Contains(
            "Cerneala.Drawing.MonoGame.Prism.Shaders.CopyComposite.mgfxo",
            resources);
    }
}
