using System.Reflection;
using BenchmarkDotNet.Running;
using Cerneala.Benchmarks;

namespace Cerneala.Tests.SdlGpu;

public sealed class CerberusBenchmarkFidelityTests
{
    [Fact]
    public void GrowthBeyondInitialCapacityBenchmarkGrowsTheStorageInEveryIteration()
    {
        // BenchmarkDotNet's own converter selects the [IterationSetup] of the benchmark. With an iteration setup the
        // in-process job runs one invocation per iteration on one instance (InvocationCount=1 in the 2026-09-02 report).
        BenchmarkRunInfo runInfo = BenchmarkConverter.TypeToBenchmarks(typeof(CerberusPlanningBenchmarks));
        BenchmarkCase growth = Assert.Single(
            runInfo.BenchmarksCases,
            benchmark => benchmark.Descriptor.WorkloadMethod.Name == nameof(CerberusPlanningBenchmarks.GrowthBeyondInitialCapacity));
        MethodInfo setupMethod = Assert.IsAssignableFrom<MethodInfo>(growth.Descriptor.IterationSetupMethod);
        CerberusPlanningBenchmarks benchmarks = new();
        // Delegates, not MethodInfo.Invoke: Invoke boxes the int result, which would count as an allocation.
        Action setup = setupMethod.CreateDelegate<Action>(benchmarks);
        Func<int> workload = growth.Descriptor.WorkloadMethod.CreateDelegate<Func<int>>(benchmarks);

        long[] allocatedBytes = new long[4];
        for (int iteration = 0; iteration < allocatedBytes.Length; iteration++)
        {
            setup();
            long before = GC.GetAllocatedBytesForCurrentThread();
            workload();
            allocatedBytes[iteration] = GC.GetAllocatedBytesForCurrentThread() - before;
        }

        // 512 quads need 2,048 vertices and 512 draws; the initial capacity is 1,024 vertices and 256 draws,
        // so every iteration that starts from initial-capacity storage grows it.
        for (int iteration = 0; iteration < allocatedBytes.Length; iteration++)
        {
            Assert.True(
                allocatedBytes[iteration] > 0,
                $"Iteration {iteration + 1} of GrowthBeyondInitialCapacity (iteration setup {setupMethod.Name}) grew no storage. " +
                $"Allocated bytes per iteration: {string.Join(", ", allocatedBytes)}.");
        }
    }
}
