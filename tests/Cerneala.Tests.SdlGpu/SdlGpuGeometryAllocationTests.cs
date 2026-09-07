using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Tests.Drawing.SdlGpu;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class SdlGpuGeometryAllocationTests
{
    [SdlNativeFact]
    public void WarmGeometryUploadsDoNotAllocateAcrossTheFrameRing()
    {
        using SdlDrawingFixture fixture = new(16, 16);
        SdlGpuVertex[] vertices =
        [
            new(Vector2.Zero, Vector2.Zero, Vector4.One),
            new(Vector2.UnitX, Vector2.UnitX, Vector4.One),
            new(Vector2.UnitY, Vector2.UnitY, Vector4.One)
        ];
        int[] indices = [0, 1, 2];
        long allocated = 0;
        for (int frame = 0; frame < 24; frame++)
        {
            fixture.Session.BeginFrame(Color.Transparent);
            try
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                SdlGpuGeometryBinding binding = fixture.Session.GeometryUploadArena.UploadGeometry(
                    fixture.Session, vertices, indices);
                long delta = GC.GetAllocatedBytesForCurrentThread() - before;
                if (frame >= 8) allocated += delta;
                Assert.NotEqual(0, binding.VertexBuffer);
                Assert.NotEqual(0, binding.IndexBuffer);
                Assert.Equal(0u, binding.VertexOffset);
                Assert.Equal(0u, binding.IndexOffset);
            }
            finally { fixture.Session.CompleteFrame(present: false); }
        }
        Assert.Equal(0, allocated);
    }
}
