# Cerneala Relay versus WPF Dispatcher

This WPF project benchmarks equivalent queueing workloads on `UiRelay` and
`System.Windows.Threading.Dispatcher` in the same BenchmarkDotNet process.

Both implementations own a dedicated STA thread. During enqueue benchmarks the
owner threads do not pump, so WPF cannot execute work concurrently while
Cerneala is still queueing it. Drain benchmarks prequeue the same number of
normal-priority no-op callbacks and then explicitly pump one batch. WPF uses a
controlled `DispatcherFrame`; Cerneala uses one `UIRoot.ProcessFrame` with a
large enough Relay budget to drain the complete batch.

Run correctness smoke checks from the repository root:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.WpfDispatcherBenchmarks\Cerneala.WpfDispatcherBenchmarks.csproj -- --smoke
```

Run the complete comparison:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.WpfDispatcherBenchmarks\Cerneala.WpfDispatcherBenchmarks.csproj -- --filter "*" --exporters markdown --artifacts .\benchmarks\results\wpf-dispatcher-comparison
```

The checked-in same-machine baseline and independent verification run are in
`../results/2026-07-14-wpf-dispatcher-comparison/`.

The benchmark covers single-operation `Post`/`BeginInvoke`, task-returning
`InvokeAsync`, drains of 1/100/1,024 callbacks, and 10,000 enqueues from 1/2/4/8
producers. WPF is the BenchmarkDotNet baseline in every class.

This is intentionally a comparison of the overlapping queue-and-dispatch
contract. It does not claim that Relay implements WPF priorities, timers,
nested message loops, Win32 message integration, or the rest of WPF's broader
Dispatcher surface.
