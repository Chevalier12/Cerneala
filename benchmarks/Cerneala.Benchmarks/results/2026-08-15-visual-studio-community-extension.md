# Visual Studio Community extension performance - 2026-08-15

## Environment

- Visual Studio: Community 18.0
- Installation: C:\Program Files\Microsoft Visual Studio\18\Community (18.9.12105.275)
- Processor: AMD EPYC 9354 32-Core Processor (8 logical processors)
- Memory: 15.98 GiB
- Operating system: Microsoft Windows NT 10.0.26200.0
- Runtime duration: 101.00 seconds
- Automation: hidden Experimental Instance, DTE/Visual Studio editor APIs only; no global keyboard, mouse, foreground or clipboard input.

## Budgets and measurements

| Workspace | Metric | Value | Budget | Unit | Result |
| --- | --- | ---: | ---: | --- | --- |
| fixture | host-ready-from-process-start | 1808.448 | - | wall-ms | GREEN |
| fixture | extension-load | 117.144 | - | wall-ms | GREEN |
| fixture | provider-activation-cpu | 15.625 | 100.000 | cpu-ms | GREEN |
| fixture | server-ready-cold | 833.196 | 2000.000 | wall-ms | GREEN |
| fixture | first-completion-cold | 1357.454 | 2500.000 | wall-ms | GREEN |
| fixture | first-diagnostics | 16406.959 | - | wall-ms | GREEN |
| fixture | editor-warm-completion-min | 25.314 | - | wall-ms | GREEN |
| fixture | editor-warm-completion-p50 | 29.411 | - | wall-ms | GREEN |
| fixture | editor-warm-completion-p95 | 40.887 | - | wall-ms | GREEN |
| fixture | editor-warm-completion-max | 42.584 | - | wall-ms | GREEN |
| fixture | editor-warm-diagnostics-min | 139.738 | - | wall-ms | GREEN |
| fixture | editor-warm-diagnostics-p50 | 155.377 | - | wall-ms | GREEN |
| fixture | editor-warm-diagnostics-p95 | 171.415 | - | wall-ms | GREEN |
| fixture | editor-warm-diagnostics-max | 186.977 | - | wall-ms | GREEN |
| fixture | server-restart | 352.272 | - | wall-ms | GREEN |
| fixture | solution-reload | 1003.578 | - | wall-ms | GREEN |
| Cerneala.slnx | solution-open | 1019.885 | - | wall-ms | GREEN |
| Cerneala.slnx | extension-load | 65.986 | - | wall-ms | GREEN |
| Cerneala.slnx | provider-activation-cpu | 0.000 | 100.000 | cpu-ms | GREEN |
| Cerneala.slnx | server-ready-cold | 964.291 | 2000.000 | wall-ms | GREEN |
| Cerneala.slnx | first-completion-cold | 1922.658 | 2500.000 | wall-ms | GREEN |
| Cerneala.slnx | first-diagnostics | 32813.260 | - | wall-ms | GREEN |
| Cerneala.slnx | editor-warm-completion-min | 149.989 | - | wall-ms | GREEN |
| Cerneala.slnx | editor-warm-completion-p50 | 181.527 | - | wall-ms | GREEN |
| Cerneala.slnx | editor-warm-completion-p95 | 228.241 | - | wall-ms | GREEN |
| Cerneala.slnx | editor-warm-completion-max | 229.622 | - | wall-ms | GREEN |
| Cerneala.slnx | editor-warm-diagnostics-min | 264.070 | - | wall-ms | GREEN |
| Cerneala.slnx | editor-warm-diagnostics-p50 | 264.781 | - | wall-ms | GREEN |
| Cerneala.slnx | editor-warm-diagnostics-p95 | 311.359 | - | wall-ms | GREEN |
| Cerneala.slnx | editor-warm-diagnostics-max | 342.310 | - | wall-ms | GREEN |
| Cerneala.slnx | solution-reload | 1663.214 | - | wall-ms | GREEN |

Cold gates are provider activation under 100 ms CPU in devenv, server ready under 2,000 ms and first useful completion under 2,500 ms. The editor warm rows measure end-to-end Visual Studio presentation latency and are reported without reusing server-only budgets. The real JSON-RPC full-solution probe enforces the inherited LSP gates: completion p95 under 100 ms and diagnostics p95 under 200 ms.

## Soak memory

| Checkpoint | devenv private MiB | server private MiB | server processes |
| --- | ---: | ---: | ---: |
| open-close-0 | 859.17 | 259.07 | 1 |
| open-close-50 | 843.09 | 259.04 | 1 |
| open-close-100 | 847.77 | 258.98 | 1 |
| edits-0 | 847.80 | 258.98 | 1 |
| edits-500 | 848.34 | 258.98 | 1 |
| edits-1000 | 824.21 | 258.98 | 1 |

The soak ran 100 document open/close cycles and 1,000 editor changes. Plateau is evaluated over the second half with limits of 96 MiB for devenv and 32 MiB for the bundled server.

## Resilience

| Scenario | Result | Evidence |
| --- | --- | --- |
| lsp-warm-budgets-full-solution | GREEN | real JSON-RPC: completion p95 < 100 ms; diagnostics p95 < 200 ms |
| extension-disabled-api | GREEN | responsive=True; ownedServerCount=0; observationSeconds=5 |
| shutdown-no-process-leak | GREEN | observedServerPids=4376,9700,12260,13072,17484; all exited |
| server-unavailable | GREEN | responsive=True; ownedServerCount=0; observationSeconds=12 |

The in-process matrix also covered a bounded server restart, solution close/reopen and an intentional C# build failure followed by repair. The disabled probe used Visual Studio's IVsExtensionManager.Disable/Enable API with the required restart; the unavailable-server probe temporarily removed only the isolated Experimental Instance server executable and restored it afterward.

## Result

**GREEN** - 30 in-process checks and 4 external resilience checks. Raw measurements are stored next to this report in 2026-08-15-visual-studio-community-extension.json.
