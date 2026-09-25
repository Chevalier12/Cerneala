# Logical image domain: controlled native A/B

**Status: diagnosis, not a renderer fix.** The current shader source, its three
generated fragment artifacts, and `Shaders/artifacts.json` are byte-exact
original A (hashes in `target-baseline-hashes.json`). The generic `linear
centroid` variant B was used only in guarded diagnostic windows, then removed.
The current Release backend and SDL test-copy DLLs both have SHA-256
`0517DC49103BE4297C7429BF45CE1A2002AA25E8CAD97DEB6C0FDE8E4B5DF7C0`.
The new test file is
`tests/Cerneala.Tests.SdlGpu/NativeDrawingInterpolationBoundaryTests.cs`,
SHA-256 `5403B901E31FEE3DCB9884BD986F3191A7FAA17C0628AA46A8D32CD8ADD90DD7`.
No other renderer production file was changed for this experiment.

## Scenario and observations

The native fixture uses a 20×20 synthetic opaque RGBA atlas with a 16×16
source crop at `(2,2)`. Crop texels vary in X, Y, and a checker channel;
outside-crop texels are contrasting magenta. A separate 16×16 image contains
the exact cropped bytes and clamps at its own image boundary. This makes
atlas-versus-extracted output disagreement an observable outside-crop sample,
not an assumed atlas-art flaw. Both paths use the same image command,
destination, sampler and Clamp address mode. The low-level native SDL
`SdlDrawingFixture` returns GPU readback pixels; no OS or application-window
screenshot API was used.

The fractional crop destination is `(8.275,8.275,32,32)` in a 64×64 target.
The fixture compares right/bottom edge bands at pixel 40 away from the image's
internal diagonal, and a safe interior region. Direct rendering uses a
one-sample window. The offscreen path records the same draw into a
`RenderSurface2D` and composites it one-to-one into that window. The native
window's actual count is logged as `One`; the earlier allocation probe on the
same Windows device established the SDL offscreen surface path selected
`Eight`, but this test has no non-reflective accessor to its cached target.

| Crop comparison | Original A | Generic centroid B |
| --- | ---: | ---: |
| Point, direct, external edge | 0 differing pixels | 0 |
| Point, offscreen + composite, external edge | **56** | 0 |
| Point, offscreen + composite, safe interior | 0 | 0 |
| Linear, offscreen + composite, external edge | 56 | 56 |

The Point offscreen A difference is guard-magenta contamination at the
right/bottom partially covered edge. The extracted-image control instead
samples its own crop-border texel. The Linear differences are a **measurement
control**, not an assertion that Linear must isolate a packed atlas; its
filter footprint can include neighbors. The current permanent Point edge
assertion is intentionally RED on A: `logical-domain-a-contract-red-06.log`
and `logical-domain-a-contract-red-06.trx` (1 pass, 1 intended failure).

For the interior experiment, the same atlas crop and affine UV plane were
mapped to the same fractional `(8.5,8.5,32,32)` quad through four explicit
image meshes: standard TL–BR diagonal `[0,1,2,0,2,3]`, alternate TR–BL
diagonal `[0,1,3,1,2,3]`, a center fan, and a 2×2 grid. The grid/fan vertex
positions and UVs are affine interpolation of the **same** four corners.
Comparison excludes the quad's external edge. Native window diagnostics
reported `Eight` samples for both A and B.

| Interior comparison to standard mesh | Original A Point / Linear | B Point | B Linear |
| --- | ---: | ---: | ---: |
| Alternate diagonal | 0 / 0 | **26** | **47** |
| Center fan | 0 / 0 | **13** | **27** |
| 2×2 grid | 0 / 0 | **67** | **53** |

B Point at internal pixel `(12,12)` changed from
`{R=35,G=43,B=224}` to `{R=29,G=37,B=32}` solely by switching the
diagonal; the 2×2 grid also produces mixed checker values at its internal
seams. The permanent Point topology assertion is intentionally RED on B:
`logical-domain-b-contract-red-08.log` and TRX (1 pass, 1 intended failure).
Linear differences are logged but not assigned a new unapproved tolerance or
made an assertion in this diagnostic.

Commands for both asserted runs, from the repository root, differed **only**
in the byte-verified shader A/B variant and a forced Release backend rebuild:

```powershell
$env:CERNEALA_SDL_NATIVE_TESTS='1'
$env:DefaultItemExcludesInProjectFolder='artifacts/**'
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj --configuration Release --no-restore -m:1 '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~NativeDrawingInterpolationBoundaryTests' --logger 'console;verbosity=detailed'
```

The actual runs also used unique TRX log names shown above. Prior
measurement-only runs (`logical-domain-a-initial-01.log`,
`logical-domain-b-initial-03.log`) had the same matrix and no contract
assertions. B rebuild evidence is `logical-domain-b-contract-rebuild-07.log`.
After the B test, A was restored from checked original bytes; the repository
compiler verified all 8 artifacts (`logical-domain-a-restored-verify-04.log`),
and a final forced Release backend rebuild plus SDL test-project build passed
(`logical-domain-a-final-rebuild-09.log`,
`logical-domain-a-testcopy-build-10.log`). The five target paths are clean in
targeted Git status. The backend's temporary sample-count probe remains
byte-exact absent.

## Interpretation and design boundary

Observed A behavior violates the chosen Point crop-domain regression at the
external partially covered edge. Observed B behavior violates the chosen
equivalent-affine-mesh Point interior regression: changing only triangle
connectivity changes fully covered logical-image pixels. This is a stronger
discriminator than calling 47 previous conformance differences harmless.
It supports the hypothesis that B's triangle-local centroid interpolation
moves UVs along **internal** diagonals, while A's ordinary interpolation can
evaluate outside the source crop at an **external** partially covered edge.
The experiments do not directly read the RenderSurface2D intermediate target;
the final offscreen result includes the one-to-one Linear composite. A
non-reflective accessor for that cached target does not exist, so the exact
first contaminated stage remains unmeasured here.

The existing shared Drawing fragment shader receives a UV and color, but no
source-crop or logical-primitive bounds. `DrawImage` owns source/flip/rotation
geometry; image meshes can specify arbitrary topology and UVs; sprite batches
and surface composites share this shader. Therefore a domain-aware solution
may require more per-draw information or a different interpolation/coverage
strategy, with effects on batching, shader inputs, and renderer performance.
None of those costs has been measured. Default per-sample shading could
multiply fragment work by the selected sample count (up to eight here), and
is not authorized. This evidence does **not** authorize unconditional UV
clamping, half-texel offsets, atlas extrusion, camera snapping, a composite
exemption, or changed reference/tolerance expectations.

Before a production design, determine how the logical image domain is
represented for cropped images versus arbitrary meshes, flips, rotations,
partial alpha and nonrectangular geometry; define what Point guarantees at
external coverage, preserve center evaluation inside equivalent quads, and
measure performance if the selected approach changes shader invocations or
batching. The full native SDL gate, historical Drawing/Prism corpus, full
solution, and Linux/macOS runtime gates remain unpassed for any proposed fix.
