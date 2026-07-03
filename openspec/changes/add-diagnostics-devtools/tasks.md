## 1. Diagnostics Snapshots

- [x] 1.1 Add frame, layout, render, and input diagnostics snapshot APIs under `UI/Diagnostics`.
- [x] 1.2 Expose any narrow read-only retained state needed by diagnostics without adding backend references.

## 2. Dumpers and Tracing

- [x] 2.1 Add dirty tree and element tree dumpers with deterministic text output.
- [x] 2.2 Add render cache dumper support for root and element cache state.
- [x] 2.3 Add routed event tracing that computes direct, bubble, and tunnel paths.
- [x] 2.4 Add style tracing over existing style diagnostics and property value sources.

## 3. Retained Debug UI and Playground

- [x] 3.1 Add retained debug overlay and debug adorner primitives.
- [x] 3.2 Add a diagnostics playground sample and register it in the sample selector.

## 4. Tests and Roadmap

- [x] 4.1 Add focused tests for frame diagnostics, dirty tree dumping, element tree dumping, render cache dumping, routed event tracing, and style tracing.
- [x] 4.2 Add or update boundary/roadmap tests proving diagnostics are backend-neutral and roadmap section 19 is complete.
- [x] 4.3 Mark `ROADMAPv2.md` section 19 files, tests, acceptance checklist, and implementation order item complete.
- [x] 4.4 Run `openspec validate add-diagnostics-devtools --strict` and `dotnet test`.
