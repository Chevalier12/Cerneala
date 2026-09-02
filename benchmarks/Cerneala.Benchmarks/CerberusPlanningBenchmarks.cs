using BenchmarkDotNet.Attributes;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Benchmarks;

[MemoryDiagnoser]
[InProcess]
[WarmupCount(3)]
[IterationCount(5)]
public class CerberusPlanningBenchmarks
{
    private static readonly int[] QuadIndices = [0, 1, 2, 0, 2, 3];
    private readonly Cerberus cerberus = new();
    private readonly SdlGpuRenderTarget target = new(
        1,
        2,
        256,
        256,
        SdlGpuTextureFormat.R8G8B8A8Unorm,
        SdlGpuSampleCount.One);
    private readonly CerberusBatchKey first = Key(1);
    private readonly CerberusBatchKey second = Key(2);

    [IterationSetup]
    public void Reset()
    {
        cerberus.Discard();
        cerberus.Begin(target);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = 1_024)]
    public int HomogeneousMergeAndIndexRebasing()
    {
        for (int draw = 0; draw < 1_024; draw++)
        {
            cerberus.Allocate(4, QuadIndices, first);
        }
        return 1_024;
    }

    [Benchmark(OperationsPerInvoke = 1_024)]
    public int AlternatingEnqueue()
    {
        for (int draw = 0; draw < 1_024; draw++)
        {
            cerberus.Allocate(4, QuadIndices, (draw & 1) == 0 ? first : second);
        }
        return 1_024;
    }

    [Benchmark(OperationsPerInvoke = 512)]
    public int GrowthBeyondInitialCapacity()
    {
        for (int draw = 0; draw < 512; draw++)
        {
            cerberus.Allocate(4, QuadIndices, (draw & 1) == 0 ? first : second);
        }
        return 512;
    }

    [Benchmark(OperationsPerInvoke = 512)]
    public int AlternatingEnqueueAndDiscard()
    {
        for (int draw = 0; draw < 512; draw++)
        {
            cerberus.Allocate(4, QuadIndices, (draw & 1) == 0 ? first : second);
        }
        cerberus.Discard();
        return 512;
    }

    private static CerberusBatchKey Key(nint texture) => new(
        DrawPrimitiveTopology.TriangleList,
        texture,
        DrawSamplingMode.Point,
        DrawAddressMode.Clamp,
        DrawBlendMode.Normal,
        SdlGpuStencilMode.Disabled,
        0,
        new SdlRect(0, 0, 256, 256),
        SdlGpuColorWriteMask.All);
}
