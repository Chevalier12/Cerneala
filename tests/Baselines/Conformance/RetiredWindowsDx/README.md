# Historical WindowsDX references

Captured on 2026-09-05 before removal of the legacy backend. These are evidence,
not an executable dependency or a requirement to retain the legacy renderer.

- 132 resource-free Prism catalog entries: 96 x 72 pixels, coordinate scale 1,
  two 16 ms preview pumps, unchanged `CatalogScene` fixtures.
- Drawing API showcase: 500 x 464 pixels, coordinate scale 1, two 16 ms preview
  pumps, unchanged `DrawingApiShowcase` fixture.
- Capture used the application-owned `DesignPreviewSession.SaveScreenshot`
  path, which delegates to `Window.SaveScreenshot`.
- Every image was compared against a fresh native SDL_GPU capture on the same
  machine. All 133 comparisons passed the existing per-channel RGBA limits:
  MAE <= 1.0, P99 <= 10, maximum <= 49. Results are in `comparison.json`.
- The temporary export test was removed after successful export. Normal tests
  only read these references; they cannot regenerate or bless replacements.

The two pre-existing opaque-stroke occlusion failures are separate contract
tests, not included as references here. Their expectations remain independent
of the legacy implementation's output.

A future mismatch requires investigating the semantic contract. Do not relax
tolerances or replace a reference merely to make a test pass.
