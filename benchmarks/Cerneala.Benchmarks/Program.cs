using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Cerneala.Benchmarks;

if (args is ["--prism-retained-cache"])
{
    PrismRetainedCacheBenchmarkRunner.Run();
    return;
}

if (args is ["--prism-sdlgpu-comparison"])
{
    PrismSdlGpuComparisonBenchmarkRunner.Run();
    return;
}

if (args is ["--language-core-gate"])
{
    CernealaLanguageBenchmarkGate.Run();
    return;
}

if (args is ["--aspect-metrics", string aspectMetricsPath])
{
    AspectResolutionBenchmarkMetricsRunner.Run(aspectMetricsPath);
    return;
}

if (args is ["--aspect-visual-diff", string baselinePath, string actualPath, string visualReportPath])
{
    AspectVisualConformanceRunner.Run(baselinePath, actualPath, visualReportPath);
    return;
}

ManualConfig config = ManualConfig
    .Create(DefaultConfig.Instance)
    .WithBuildTimeout(TimeSpan.FromMinutes(10));
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
