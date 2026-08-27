# SDL_GPU Stage 5 drawing conformance

Date: 2026-08-26

## Scope and implementation evidence

- `SdlGpuDrawingBackend.HandledCommandKinds` enumerates every current `DrawCommandKind`; `DrawingCommandCoverageTracksTheCompleteCoreEnum` fails when the core enum and backend coverage diverge. `BeginPrism`/`EndPrism` remain structural commands here because Prism execution belongs to Stage 7.
- Geometry is produced through the shared core path/stroke tessellators. Vertex and index data use reusable growing GPU buffers and a reusable transfer buffer; compatible adjacent batches merge without reordering commands.
- Pipeline and sampler caches are per shared GPU device. Window render targets remain per session, while sampled images, shaped-text atlas pages, pipelines, and samplers are shared by windows using that device.
- Solid, linear-gradient, radial-gradient, and image brushes use the core brush descriptors and brush-space mapping. Raster images are decoded with the existing Skia capability and uploaded without SDL_image.
- Solid shaped text uses an eight-phase subpixel key and three channel-masked layers packed into a bounded per-device atlas (maximum eight 1024 x 1024 pages). Atlas pages track active frames before LRU reuse. Non-solid text preserves brush colorization. Shaping, kerning, baseline, measurement, and rasterization remain in the existing core text pipeline.
- `RenderSurface2D` owns independent multisampled color/depth targets plus a single-sample resolve texture, retains unchanged frames, recreates after resize, preserves nested state, and can be sampled from windows sharing the device.

## Visual oracle

Both sides render the same `DrawingApiShowcase` at fixed dimensions and scale. WindowsDX is captured through `DesignPreviewSession.SaveScreenshot`; SDL_GPU is captured through `Window.SaveScreenshot`. No OS screen-copy API is used. The comparison is over normalized RGBA channel bytes and emits both source images, a heatmap, and a report on failure.

Command:

```powershell
$env:CERNEALA_SDL_NATIVE_TESTS='1'
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter FullyQualifiedName~SdlGpuDrawingConformanceTests --no-restore --logger "console;verbosity=detailed"
```

Result:

| Metric | Actual | Required |
|---|---:|---:|
| Mean absolute error | 0.0019 | <= 1.0 |
| Per-channel P99 | 0 | <= 10 |
| Maximum absolute delta | 17 | < 50 |

The last material mismatch was traced to sample-count negotiation: WindowsDX selected 8x MSAA while SDL_GPU requested 4x. SDL_GPU now requests 8x and retains its existing 8 -> 4 -> 2 -> 1 capability fallback.

## Resource and regression verification

- Focused drawing backend tests: 8 passed. They cover command inventory, semantic batching, nested scissor/stencil restoration, image invalidation, a shared per-device text atlas and subpixel phase reuse across two windows, retained/resized and multisampled `RenderSurface2D`, and stable resource counts across 30 repeated frames followed by complete disposal.
- Full SDL Release suite: 38 passed, 3 native tests skipped by the headless/default opt-in guard.
- Windows native SDL lifetime/multi-window/screenshot subset: 3 passed.
- Core Drawing Release suite: 1026 passed, 1 native SDL oracle skipped by the default opt-in guard. The same oracle was run explicitly and passed with the metrics above.
- Full solution Release build: succeeded with 0 warnings and 0 errors.
- Roslyn index: valid, 3424 documents, 87796 symbols, 355334 references, and no dirty indexed files. The three retained index warnings are the two known unmatched `Cerneala.Language` metadata references and the oversized binary `Drawing/Prism/Filters/Assets/bluenoise.bin`.

Native Linux/macOS smoke execution is intentionally omitted because no runners are available. Their SDL_GPU implementation and packaging support remain in scope; only native smoke execution is waived.
