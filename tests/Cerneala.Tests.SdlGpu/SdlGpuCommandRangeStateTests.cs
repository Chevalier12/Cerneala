using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuCommandRangeStateTests
{
    [Fact]
    public void ResetRestoresEveryRootStackAndDropsAnAbandonedCompositingChild()
    {
        SdlGpuRenderTarget first = Target(1, 16, 16);
        SdlGpuRenderTarget next = Target(3, 32, 24);
        SdlGpuRenderTarget child = Target(5, 16, 16);
        SdlGpuDrawingBackend.RenderState root = SdlGpuDrawingBackend.RenderState.Create(first, 1);
        SdlGpuDrawingBackend.CommandRangeState range = new(first, root);
        root.Transforms.Add(Matrix3x2.CreateTranslation(2, 3));
        root.Opacities.Add(.25f);
        root.Blends.Add(DrawBlendMode.Screen);
        root.Scissors.Add(new SdlRect(1, 2, 3, 4));
        root.Clips.Add(SdlGpuDrawingBackend.ClipEntry.ForScissor(root.Scissor));
        root.StencilDepth = 2;
        range.CompositingScopes.Add(new(first, root, child, .5f, DrawBlendMode.Screen, DrawCommandKind.PushLayer));
        range.Target = child;
        range.State = SdlGpuDrawingBackend.RenderState.Create(child, 1);

        range.Reset(next, 2);

        Assert.Same(next, range.RootTarget);
        Assert.Same(next, range.Target);
        Assert.Same(root, range.State);
        Assert.Equal(Matrix3x2.Identity, Assert.Single(root.Transforms));
        Assert.Equal(1, Assert.Single(root.Opacities));
        Assert.Equal(DrawBlendMode.Normal, Assert.Single(root.Blends));
        Assert.Equal(new SdlRect(0, 0, 32, 24), Assert.Single(root.Scissors));
        Assert.Empty(root.Clips);
        Assert.Equal(0, root.StencilDepth);
        Assert.Equal(SdlGpuStencilMode.Disabled, root.StencilMode);
        Assert.Empty(range.CompositingScopes);
    }

    [Fact]
    public void WarmResetDoesNotAllocateOrReplaceTheOwnedState()
    {
        SdlGpuRenderTarget target = Target(1, 16, 16);
        SdlGpuDrawingBackend.RenderState root = SdlGpuDrawingBackend.RenderState.Create(target, 1);
        SdlGpuDrawingBackend.CommandRangeState range = new(target, root);
        for (int iteration = 0; iteration < 8; iteration++) range.Reset(target, 1);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int iteration = 0; iteration < 64; iteration++) range.Reset(target, 1);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Same(root, range.State);
    }

    private static SdlGpuRenderTarget Target(int handle, int width, int height) => new(
        handle, handle + 1, width, height,
        SdlGpuTextureFormat.R8G8B8A8Unorm, SdlGpuSampleCount.One);
}
