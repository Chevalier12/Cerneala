# Final verification

## Test suites

Final targeted projects:

- `Cerneala.Tests.SourceGen`: 476 passed, 0 failed.
- `Cerneala.Tests.Language`: 181 passed, 0 failed.
- `Cerneala.Tests`: 3,131 passed, 0 failed, 2 declared native visual skips.

Final `dotnet test .\Cerneala.slnx`:

| Project | Passed | Skipped | Failed |
| --- | ---: | ---: | ---: |
| `Cerneala.Tests` | 3,131 | 2 | 0 |
| `Cerneala.Tests.SourceGen` | 476 | 0 | 0 |
| `Cerneala.Tests.Language` | 181 | 0 | 0 |
| `Cerneala.Tests.LanguageServer` | 40 | 0 | 0 |
| `Cerneala.Tests.PreviewHost` | 12 | 0 | 0 |
| `Cerneala.Tests.VisualStudio` | 47 | 0 | 0 |
| `Cerneala.Tests.SdlGpu` | 63 | 4 | 0 |

The first solution attempt exposed a harness collision: `StructureProtocolTests` spawned a Release build that wrote the repository's `obj/Release` while the solution build used the same output. The fixture now passes `--artifacts-path <temporary workspace>/artifacts`; its focused test and the final concurrent solution run are GREEN.

## Build and formatting

- Full `dotnet build .\Cerneala.slnx -c Release --no-restore`: 0 warnings, 0 errors.
- `git diff --check`: clean; Git only reports existing LF-to-CRLF checkout warnings.
- Formatter verification restricted to every C# file modified by the Aspect goal: GREEN.
- The unrestricted repository formatter was also run and remains RED on unchanged, out-of-scope baseline files such as `SdlGpuDrawingBackend.cs`, `PrismInstance.cs`, LanguageServer formatting fixtures, and Prism lens tests. The goal files reported by that run were formatted and the scoped rerun is GREEN; unrelated formatter debt was not rewritten.

## API and documentation

- Strict final ApiCompat output: `final-api-diff.txt`.
- Complete classification/justification: `final-api-diff.md`.
- Canonical documentation manifest: 1,075 entries, 0 duplicate names, 0 missing pages, 0 missing sources.
- Official Visual Studio manifest test: GREEN.
- `docs/aspect-system.md` describes one runtime data flow.
- The 2026-07-09 markup plan/spec and the 2026-07-10 template plan are explicitly marked superseded historical documents.

## Repository and architecture gates

- `MarkupAspectResource` source and documentation page are absent.
- RoslynIndexer symbol search reports no legacy type, executor methods, or authoring-specific value-source symbols.
- Exact production text search reports no legacy identifiers.
- `UIRoot` contains no markup Aspect discovery, target matching, or cascade.
- Motion transactions remain in the existing Aspect-to-Motion bridge; generated event/Motion/binding/input sidecars remain owned by their dedicated subsystems.
- RoslynIndexer forced full rebuild: valid, 0 dirty files; `doctor` passes every applicable check. Seven reported warnings are the two established project-reference metadata warnings plus five intentionally skipped oversized shader/binary/frame-report files.

## Performance and visual gates

See `stage6-results.md`. The authoritative optimized benchmark, deterministic counters, final Presentation frame report, and exact 1600x900 `Window.SaveScreenshot` comparisons are archived in this directory. Aspect Studio and Build-Time Markup/template captures have zero differing pixels.
