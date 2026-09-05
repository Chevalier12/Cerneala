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

if (args is ["--tilemap-baseline", string tileMapBaselinePath])
{
    TileMapBaselineBenchmarkRunner.Run(tileMapBaselinePath);
    return;
}

if (args is ["--tilemap-stage4", string tileMapStage4Path])
{
    TileMapStage4BenchmarkRunner.Run(tileMapStage4Path);
    return;
}

if (args is ["--tilemap-stage4-backends", string tileMapStage4BackendPath])
{
    TileMapStage4BackendProfileRunner.Run(tileMapStage4BackendPath);
    return;
}

if (args is ["--collision-stage0", string collisionStageZeroPath])
{
    CollisionStageZeroBenchmarkRunner.Run(collisionStageZeroPath);
    return;
}

if (args is ["--collision-stage2", string collisionStageTwoPath])
{
    CollisionStageTwoBenchmarkRunner.Run(collisionStageTwoPath);
    return;
}

if (args is ["--sprite-animation", string spriteAnimationPath])
{
    SpriteAnimationBenchmarkRunner.Run(spriteAnimationPath);
    return;
}

if (args is ["--scene-debug-overlay", string sceneDebugOverlayPath])
{
    SceneDebugOverlayBenchmarkRunner.Run(sceneDebugOverlayPath);
    return;
}

ManualConfig config = ManualConfig
    .Create(DefaultConfig.Instance)
    .WithBuildTimeout(TimeSpan.FromMinutes(10));
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
