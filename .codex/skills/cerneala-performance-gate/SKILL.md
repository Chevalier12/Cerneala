---
name: cerneala-performance-gate
description: Reproduce, diagnose, optimize, and verify observable Cerneala performance defects against a user-specified acceptance gate. Use when the user reports lag, stutter, slow startup, excessive CPU, allocations, retained memory, GPU cost, or frame-time spikes and supplies a concrete P95, P99, allocation, memory, startup, throughput, or similar threshold. Do not use for speculative optimization without an observable problem and an explicit measurable gate.
---

# Cerneala Performance Gate

Turn an observable performance failure into a fixed measurement contract, find the owning bottleneck, and continue until the exact gate passes without weakening correctness, architecture, or visual fidelity.

## 1. Freeze the Gate

Record the gate before changing production code:

- the exact user-visible scenario and deterministic input sequence;
- the metric, statistic, unit, and pass threshold;
- backend, platform, hardware, display mode, build configuration, and relevant runtime settings;
- startup versus steady-state scope, warmup, sample duration or frame count, and number of fresh-process runs;
- correctness, visual-fidelity, latency, and memory constraints that must remain unchanged.

Treat these details as one measurement contract. Do not change the workload, percentile, sample window, warmup, quality, or threshold after seeing the result. If a material part is missing or admits materially different outcomes, stop and ask the user. “Make it faster” is not a gate.

Use the user's metric as written. Do not silently replace working set with managed heap, total frame time with CPU submission time, P99 with an average, or time to first correct frame with time to window creation.

## 2. Build a Faithful Measurement

- Reproduce the reported lag before optimizing. Preserve the original observable symptom separately from the numeric gate.
- Exercise the real owning path. Rendering, presentation, input, startup, and native-resource problems require the relevant real backend and frame loop, not a weaker unit benchmark.
- Prefer deterministic application automation or supported input APIs. Do not use real mouse or keyboard input when it can disrupt the user or when the user forbids it.
- Keep probes bounded and make their overhead measurable. Do not let logging, screenshots, debugger attachment, or per-frame file I/O dominate the result.
- Use identical warmup, workload, sample collection, and process lifecycle for baseline and final measurements.
- Retain the raw report and exact command outside the repository when the harness is temporary.

Run enough fresh-process repetitions to expose startup variance and enough frames or operations to make the requested percentile meaningful. Do not average away a reproducible spike or discard outliers merely because they are inconvenient.

### Temporary Deep Instrumentation Authority

The user authorizes comprehensive temporary instrumentation across every accessible layer relevant to the gate. Do not stop at existing counters when deeper evidence is needed. You may temporarily modify or instrument:

- the application, Cerneala core, Prism graph and effects, renderer, and SDL backend;
- layout, invalidation, frame scheduling, render-graph construction, batching, uploads, command recording, submission, presentation, and synchronization;
- shaders, intermediate surfaces, textures, buffers, pools, caches, pipelines, descriptors, fences, and device-loss or disposal paths;
- managed/native interop, SDL calls, platform integration, allocator and object lifetimes, and background initialization;
- CPU profilers, allocation and retained-object captures, ETW or equivalent platform traces, GPU captures, timestamp queries, validation layers, and supported driver diagnostics;
- temporary native runtime harnesses, opt-in automation branches, structured traces, counters, assertions, and resource inventories.

Instrument production source temporarily when that is the narrowest faithful observation point. Guard behavioral probes with a unique environment variable, command-line switch, or internal diagnostic entry point so normal runs remain unchanged. A diagnostic patch may be broad enough to observe the whole relevant pipeline; it must not become a speculative production redesign.

Keep the instrumentation observational and quantify its overhead. If a probe perturbs timing, allocations, synchronization, resource lifetime, output, or scheduling enough to invalidate the gate, replace it with a lower-overhead probe or measure in separate diagnostic and gate runs. Never mistake profiler overhead for product cost.

This authority ends at relevance and observability: do not inspect unrelated subsystems for sport, bypass platform security, or claim visibility into proprietary driver internals that the available APIs and tools do not expose. Before final verification, remove every temporary source branch, shader change, hook, environment variable, harness, trace sink, assertion, native artifact, and generated report from the repository. Reindex and rebuild after cleanup, then confirm the instrumented files have no residual diagnostic diff.

### Diagnostic Tool and Trace Authority

The user authorizes collecting every relevant bounded trace and installing the diagnostic tooling required to obtain decisive evidence. A missing tool is not a reason to fall back to guesswork or shallow counters. When relevant, you may acquire, install, configure, and use:

- CPU, sampling, instrumentation, allocation, heap, retained-object, dump, and native-memory profilers;
- ETW collectors and analyzers, Windows Performance Recorder/Analyzer, GPUView, event providers, symbols, and stack-walking support;
- GPU debuggers and capture tools such as PIX, RenderDoc, validation layers, shader debuggers, timestamp tooling, and vendor-supported profilers;
- native debuggers, crash and hang dump tools, SDK components, command-line utilities, driver-exposed diagnostics, and platform tracing packages;
- local or temporary scripts and converters needed to turn raw traces into reproducible measurements and inventories.

Prefer an official publisher, package manager, signed release, or otherwise verifiable source. Record the tool name, version, source, installation command, configuration, capture command, and produced artifact so another run can reproduce the evidence. Prefer portable, repository-external, or user-local installation when it provides the same fidelity; use machine-wide installation when the required tool cannot work otherwise and the environment permits it.

Tool installation is authorized, but it does not authorize disabling platform security, accepting unrelated bundled software, changing production dependencies, replacing the user's GPU driver without evidence, or concealing a required elevation, license, reboot, network, or hardware blocker. Do not uninstall or reconfigure a tool that predated the investigation. Remove investigation-only tools, services, environment changes, and large trace artifacts when safe and practical; otherwise report exactly what remains installed and why.

Capture traces in bounded windows around the deterministic scenario. Correlate clocks and markers across application, runtime, native, SDL, GPU submission, and presentation layers when possible. Run an uninstrumented copy of the frozen gate after diagnosis: a trace can establish ownership, but profiler or validation-layer overhead cannot be used as the final performance result.

## 3. Name the Metric Honestly

Separate metrics that answer different questions:

- end-to-end frame interval, CPU update/build/submit time, GPU time, and present or frame-pacing wait;
- allocations per frame, managed live heap, private working set, committed memory, resident pages, and native or GPU resource bytes;
- first window, first presented frame, and first complete correct frame;
- median, P95, P99, maximum, and missed-frame count.

A command-building P99 below 6.94 ms is not proof that the application is 144 Hz-safe if total frame time or presentation misses that budget. A passing P95 does not excuse a failing P99 when the gate includes both. State exactly what was measured.

## 4. Establish Baseline and RED

Capture a baseline with the frozen gate and report:

- every gated value;
- representative run-to-run spread;
- correctness and visual observations;
- phase, allocation, GC, cache, invalidation, draw, render-pass, and native-resource counters relevant to the symptom.

Create the smallest permanent regression test or maintained performance gate that fails for the intended reason. Confirm RED before changing production behavior. If the real backend or hardware is required and a normal test cannot express the contract, keep the deterministic runtime gate as the RED artifact and document why a weaker test would be false evidence.

## 5. Attribute Cost Before Editing

- Instrument phase boundaries and resource lifetimes until the dominant cost and its owner are evidenced.
- Distinguish the component exposing the lag, the trigger, and the subsystem owning the violated invariant.
- Consider at least one plausible owner outside the most recently modified subsystem when evidence allows.
- Use allocation profiles, retained-object roots, native resource counts, GPU captures, or startup traces according to the metric. Do not infer retained memory from allocations alone.
- After two experiments fail to support the same hypothesis, discard it and rebuild the hypothesis set.

Do not edit production code merely because a hot-looking method is plausible. Quantify its contribution to the gated metric first.

## 6. Optimize the Invariant Owner

Implement the smallest architecture-correct framework change that removes the evidenced cost.

- Fix Cerneala when Cerneala owns the cost; do not hide framework churn in a demo or application-specific workaround.
- Preserve output and behavior unless the user explicitly authorizes a tradeoff.
- Do not pass by disabling an effect, reducing resolution or samples, skipping necessary work, deferring work beyond the measurement window, changing timing semantics, or weakening the gate.
- Pool, cache, batch, or retain work only when ownership, invalidation, disposal, device loss, and bounded-growth behavior remain correct.
- Avoid replacing measured cost with unmeasured retained memory, latency, GPU work, or startup work.
- Reindex after C# or project-file modifications and follow the repository's source, test, and documentation rules.

When the correct solution requires a nontrivial algorithm choice, use the project algorithm-market workflow before committing to one. Do not add algorithmic machinery for an ordinary lifetime or invalidation bug.

## 7. Prove the Exact Gate GREEN

After each viable fix, run the unchanged gate. Continue until it passes or evidence establishes a real blocker.

Report baseline and final values side by side with absolute and relative deltas. A result is GREEN only when every requested threshold passes in the same valid run set. An improvement that still misses the threshold is not done.

Also verify that the optimization did not introduce:

- visual or semantic differences;
- unbounded memory or resource growth;
- cache staleness, flicker, black frames, or lifecycle failures;
- worse startup, input latency, frame pacing, or another gated percentile.

For renderer or effect changes, run deterministic visual conformance or pixel/color comparisons. “Looks the same” is not evidence. For memory changes, include a bounded grow-and-release sequence and distinguish plateau from leak.

## 8. Verify and Clean Up

Run, in order:

1. the RED performance regression or runtime gate;
2. the original observable reproduction;
3. focused correctness and performance tests;
4. broader affected suites and the required repository-wide gate;
5. applicable visual, startup, memory-lifetime, API, and conformance gates.

Remove temporary instrumentation, harnesses, generated reports inside the repository, and application modifications. Preserve raw evidence outside the repository only when useful and safe. Never claim human manual validation unless the user performed it.

## 9. Report the Evidence

State:

- observed symptom and frozen gate;
- baseline measurements and measurement conditions;
- root cause and the evidence that assigns ownership;
- production change and why it preserves fidelity and contracts;
- final measurements from the identical gate;
- correctness, visual, memory, focused, and full-suite verification;
- remaining uncertainty, environmental limits, and any user-validation step.

Explicitly distinguish “the requested percentile passes” from “every frame meets budget.” If an environmental disturbance invalidates a run, rerun it rather than laundering it into the aggregate. If the gate conflicts with correctness, fidelity, architecture, or physical platform limits, stop and present the evidence instead of cheating the measurement.
