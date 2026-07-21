using BenchmarkDotNet.Running;
using Cerneala.Benchmarks;

if (args is ["--prism-retained-cache"])
{
    PrismRetainedCacheBenchmarkRunner.Run();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
