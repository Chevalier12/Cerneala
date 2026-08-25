using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Cerneala.Benchmarks;

if (args is ["--prism-retained-cache"])
{
    PrismRetainedCacheBenchmarkRunner.Run();
    return;
}

if (args is ["--language-core-gate"])
{
    CernealaLanguageBenchmarkGate.Run();
    return;
}

ManualConfig config = ManualConfig
    .Create(DefaultConfig.Instance)
    .WithBuildTimeout(TimeSpan.FromMinutes(10));
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
