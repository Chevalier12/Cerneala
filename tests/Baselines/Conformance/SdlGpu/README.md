# SDL_GPU conformance references

Captured on 2026-10-20 with the SDL_GPU renderer and its distance-based edge
coverage (issue #696): fills and strokes drawn into a single-sample target
paint a one-pixel coverage ramp across their edges; multisampled targets keep
their multisample antialiasing.

- Source: base commit `ae7ffe57d2b75ec70cb0fc2e1a6dca85bbecafe0` plus the
  uncommitted issue #696 change.
- Machine: Windows 11 Pro 10.0.26200. Installed adapters: NVIDIA GeForce RTX
  2060 (driver 32.0.15.9159), Intel UHD Graphics 630 (driver 26.20.100.6911).
  The adapter and SDL_GPU driver selected by SDL were not recorded.
- 132 resource-free Prism catalog entries: 96 x 72 pixels, coordinate scale 1,
  single-sample window (`useMultisampling: false`), two 16 ms pumps, unchanged
  `CatalogScene` fixtures.
- Drawing API showcase: 500 x 464 pixels, coordinate scale 1, multisampled
  window (`useMultisampling: true`), unchanged `DrawingApiShowcase` fixture.
- Every image is the `sdlgpu.png` capture written by the unchanged
  `PrismSdlGpuPixelConformanceTests` and `SdlGpuDrawingConformanceTests`
  through `Window.SaveScreenshot`, exported with the existing
  `CERNEALA_SDL_CONFORMANCE_ARTIFACTS` artifact directory. No export code was
  added.
- Every image was compared against a fresh native SDL_GPU capture with the
  unchanged per-channel RGBA limits: MAE <= 1.0, P99 <= 10, maximum <= 49.
  All 133 comparisons passed with zero difference. Results are in
  `comparison.json`; file hashes are in `sha256.json`.

`../RetiredWindowsDx/` stays unchanged as the historical MonoGame WindowsDX
reference. The conformance tests read this folder instead.

A future mismatch requires investigating the semantic contract. Do not relax
tolerances or replace a reference merely to make a test pass.
