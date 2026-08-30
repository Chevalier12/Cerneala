using Cerneala.Backends.SdlGpu;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuPrismUniformAllocationTests
{
    [Fact]
    public void RepackingStableUniformStorageDoesNotAllocatePerPass()
    {
        SdlGpuPrismUniforms uniforms = new();
        _ = uniforms.Pack();

        long before = GC.GetAllocatedBytesForCurrentThread();
        byte[]? packed = null;
        for (int pass = 0; pass < 256; pass++)
        {
            packed = uniforms.Pack();
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        GC.KeepAlive(packed);
        Assert.Equal(0, allocated);
    }
}
