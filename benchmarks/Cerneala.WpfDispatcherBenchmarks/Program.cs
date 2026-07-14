using BenchmarkDotNet.Running;
using Cerneala.WpfDispatcherBenchmarks;

if (args is ["--smoke"])
{
    DispatcherComparisonSmoke.Run();
    Console.WriteLine("WPF Dispatcher comparison smoke checks passed.");
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
