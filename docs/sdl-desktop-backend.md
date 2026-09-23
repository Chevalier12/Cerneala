# SDL3 + SDL_GPU Desktop Backend

The SDL desktop backend combines SDL3 windowing with SDL_GPU rendering and is the sole maintained desktop backend. The source generator stays backend-neutral, and each executable selects exactly one composition at assembly level.

## Select the backend

Reference the backend and source generator from a `net8.0` executable:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RuntimeIdentifiers>win-x64;win-arm64;linux-x64;linux-arm64;osx-x64;osx-arm64</RuntimeIdentifiers>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Cerneala.csproj" />
    <ProjectReference Include="..\Cerneala.Backends.SdlGpu\Cerneala.Backends.SdlGpu.csproj" />
    <ProjectReference Include="..\Cerneala.SourceGen\Cerneala.SourceGen.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
    <AdditionalFiles Include="**\*.crn" Exclude="bin\**;obj\**" />
  </ItemGroup>
</Project>
```

Then add one assembly declaration:

```csharp
[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(
    typeof(Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend))]
```

Repeating the same SDL_GPU registration is harmless. A different windowing backend cannot be registered in the same process. The retired MonoGame and WindowsDX compositions are no longer available.

## Rendering contract

SDL3 owns windows, input, DPI, cursors, and the event pump. The renderer uses `SDL_GPU` directly: one GPU device is shared by the platform, while each window owns its swapchain and presentation session. There is no `SDL_Renderer`/`SDL_Render` path.

The SDL_GPU driver is D3D12 on Windows, Vulkan on Linux, and Metal on macOS. Shader artifacts for DXIL, SPIR-V, and MSL are compiled offline from the shared HLSL sources. The published application does not compile shaders and does not carry ShaderCross as a runtime dependency.

### Internal drawing flow

`SdlGpuDrawingBackend` translates Cerneala draw commands and state stacks into SDL_GPU geometry and state. It also owns text-atlas preparation, Prism and layer execution, render-target changes, and every barrier that requires pending geometry to be emitted. The internal `Cerberus` component owns the reusable CPU-side geometry storage, adjacent compatible-batch merging, index rebasing, geometry upload, and ordered SDL_GPU draw emission.

Cerberus is an implementation detail of `Cerneala.Backends.SdlGpu`, not a public extension point. It exposes no public `Begin`/`End` lifecycle or sorting options; `IDrawingBackend` remains the public renderer substitution contract. Each window graphics session has its own backend, geometry-upload arena, Cerberus instance, and mutable queue. Only the existing device-level drawing resources are shared across sessions.

#### Command-buffer publication and retained reuse

Retained GPU content is published against the outcome of the command buffer that records it. A newly rendered Prism target, `RenderSurface2D` target, drawing-brush capture, sampled texture, or text-atlas upload is initially **pending** under an internal token that combines the owning window session with that session's command-buffer generation. Pending content may be replayed only by the same active command buffer. It is not visible to another buffer or window merely because the underlying device or native handle is shared.

`RenderSurface3D` follows that publication boundary for its isolated color/depth target. Its raster key includes the requested generation, captured command version, logical bounds, physical dimensions, DPI scale, and target format. Replay of a captured command in the same buffer uses its completed pending target, even if its callback requested a later generation; a later buffer records that new generation. A separate camera or raster mutation after the callback is not folded into that replay allowance. A partially recorded or failed pass is never a submitted hit. The control owns only its per-session acquisitions: detach, removal of its last subscriber, root resource cleanup, and session disposal retire those targets idempotently. A source acquisition epoch rejects retained commands captured before retirement, while fresh captures can reacquire after attachment or resource replacement. Each 3D target's color, resolve, and depth textures are pinned until every referencing command buffer has submitted or been cancelled; another window's retirement flush cannot release a pending target. Device-owned 3D shader and pipeline caches remain shared until device disposal; closing one window does not retire another window's target.

A successful `SDL_SubmitGPUCommandBuffer` outcome changes eligible pending content to **submitted**, allowing later command buffers to reuse it. A failed submit, explicit cancellation, or session cleanup **abandons** that buffer's pending content: cache entries and atlas revisions that were not previously submitted must be uploaded or rendered again. Invalidation or disposal while work is pending cannot resurrect the invalidated owner during the later outcome notification. Resources referenced by pending work stay pinned until the buffer outcome is known, so pool reuse or budget eviction cannot overwrite a target or upload that an unfinished buffer still references.

Command-buffer submission is not presentation and is not GPU fence completion. `CompleteFrame(false)` still submits recorded work without presenting it, and swapchain recovery may submit one buffer before acquiring and recording a replacement. A later recovery failure therefore does not roll back valid work from an earlier successful submit. Conversely, ending drawing or recording a result does not publish it before submit succeeds. The session clears its active native handle before calling submit because SDL consumes the handle even when submission reports failure; cleanup never attempts to cancel that consumed handle. Fence-based readback and actual resource retirement remain separate lifetime steps.

The backend flushes through one coordination path. Pending resource uploads, including the text atlas, are completed before Cerberus uploads and emits queued geometry. Flush barriers cover copy passes, Prism execution, layer and `RenderSurface2D` target changes, child-target composition, clip/stencil transitions, and the end of a command range or frame. Cerberus preserves painter order and merges only immediately adjacent compatible triangle lists; it never sorts commands by texture or depth.

The device-level text atlas retains raster variants across frames within eight 1024 x 1024 RGBA pages. It grows lazily to that limit before reusing the least-recently-used inactive page; pages referenced by an unfinished frame cannot be evicted. Closing a frame releases its page references without compacting, copying, or relocating cached pixels. The maximum page payload is 32 MiB of CPU pixels plus 32 MiB of GPU pixels, excluding cache metadata and driver overhead. Pages are reused under budget pressure and released with their device-level resource owner. This policy trades bounded retention for fewer repeated rasterizations; it does not change text coverage, subpixel phases, or the fallback for requests that cannot fit in the atlas.

Non-solid text checks its existing brush-texture cache before generating CPU coverage. Gradient text retains its colorized texture; tile-brush text retains its alpha mask independently of the brush capture. Cached texture dimensions and raster origin preserve baseline placement when the same canonical phase is translated. Reusing a text mask does not freeze ImageBrush, DrawingBrush or VisualBrush content: the capture still checks the current brush commands and reapplies the mask when repainting. These textures follow the existing per-backend retain/release lifecycle and are retired when no backend uses them; they do not extend the solid-text atlas's page budget or retention policy.

SDL_GPU state caching is local to one flush and its resumed render pass. The first draw binds complete state, and subsequent draws omit only documented-safe redundant binds. A copy pass, target change, geometry upload, or other render-pass restart discards that cache so the resumed pass binds complete state again.

### Internal pipeline and geometry contracts

Vertex input and depth are explicit internal pipeline inputs owned by the SDL platform adapter; they are not inferred from shader names and are not public extension points. The retained 2D path supplies a 32-byte per-vertex layout with `Float2` attributes at offsets 0 and 8 and a `Float4` attribute at offset 16. Fullscreen Prism passes supply an empty input with zero stride. The descriptor snapshots its attribute array and rejects unsupported formats, duplicate locations, unaligned or overlapping attributes, and attributes outside the declared stride before SDL is called. `NativeSdlApi.CreateNativeGraphicsPipelineDescriptor` is the single conversion path used by real pipeline creation, so tests and runtime do not maintain separate native-layout mappings.

Depth test, depth write, and compare operation are described independently from stencil. Existing 2D and Prism pipelines explicitly disable depth test and writes and use `Always`; the retained 2D increment/test/decrement stencil behavior is unchanged. The internal normal-depth preset enables test and write with `LessOrEqual`. Render-target clear depth remains 1.0, and this foundation does not add reversed-Z or a configurable clear contract.

Geometry transfer remains session-owned. Each window session owns one `SdlGpuGeometryUploadArena` with the existing three frame slots. Cerberus uses it for retained 2D geometry; the `RenderSurface3D` executor also uses the same session arena for its 3D vertex layout and indices. Its typed front end accepts unmanaged vertex spans, views both vertices and signed 32-bit indices as bytes, and enters one shared validation, reservation, map/copy, upload, growth, and retirement path. Byte counts and binding offsets use checked arithmetic and four-byte alignment. Interleaved layouts reserve distinct regions in submission order; a replaced buffer restarts at offset zero, while a failed transfer does not make an already reserved region reusable by a later draw. The 3D path has its own color/depth targets and device-owned pipelines; sharing the transfer arena does not merge its render pass with 2D, change Cerberus batching, or change painter order.

## Runtime identifiers and native assets

### Public Graphix managed/native dependencies

The native runtime comes from [Graphix.Native 3.4.16-graphix.6](https://www.nuget.org/packages/Graphix.Native/3.4.16-graphix.6),
an independently maintained SDL fork. The managed binding is [Graphix-CS 3.4.16.1](https://www.nuget.org/packages/Graphix-CS/3.4.16.1),
a packaging fork of upstream `SDL3-CS v3.4.16.0`. It preserves namespace `SDL3`,
public class `SDL`, assembly name `SDL3-CS.dll`, and the upstream binding/generator source.
Do not reference both `Graphix-CS` and `SDL3-CS` in the same application.
The separate `SDL3-CS.*.Shadercross` packages remain build-tool dependencies,
not part of Graphix. Do not add the old `SDL3-CS.Windows`, `SDL3-CS.Linux` or
`SDL3-CS.MacOS` native providers alongside Graphix.

Both packages are public on NuGet.org. The repository's `NuGet.Config` declares
the public NuGet source without clearing user-wide package sources. A fresh
checkout restores directly; no GitHub authentication, temporary Actions artifact
or local package feed is required by developers or CI:

```powershell
dotnet restore Cerneala.slnx
```

The managed package comes from verified [Graphix-CS main CI run 34129320508](https://github.com/Chevalier12/Graphix-CS/actions/runs/34129320508),
commit `79f3958a88aad3ba354aeee8fece42ff33782d56`. The downloaded NuGet.org `.nupkg` SHA256 is
`C11770CC11D194E22D17C8CD90DCD93D0346B6ED78DDD72475F5F2CFD5C1CA1D`.
The package records this commit in its repository metadata and contains no native runtime assets.

The inherited callback generator references Roslyn 5.9. Cerneala pins SDK `10.0.400`
in `global.json` so the compiler can load it; application target frameworks remain unchanged.
Older Roslyn 5.6 compilers reject the generator with `CS9057`.

The current native package records Graphix source commit
`e912da037a59e33affc6ecd69be326265cdc748b`, built by
[Graphix CI run 34618709538](https://github.com/Chevalier12/Graphix/actions/runs/34618709538).
All six native build/test jobs and package assembly succeeded: 162 CTest entries
passed, with zero failures or skipped entries. Both separate Windows native
maximum-size gates passed 400/400 assertions. These are not cross-platform GPU
certification results.

The downloaded NuGet.org `.nupkg` SHA256 is
`3F07C5A2D32A72C05A6FE9BF05C19A2F429C2DB8B3AA3014B932537031ED7A35`.
Its NuGet repository signature is valid; all 25 entries from the verified CI
archive match byte-for-byte, with only the repository signature added. The
packaged Windows x64 DLL SHA256 is
`2958F3D36859AD71ED5CA5D16B89768212FFE31F5C87CA48212EF6C5FD45A51F`.

Graphix.6 retains compatible D3D12 graphics root bindings when pipeline objects
share the same native root signature. First-pass binding, resource changes and
descriptor-heap rotation retain their existing invalidation rules. There is no
managed binding or public SDL API/ABI change. The exact packaged Windows x64
DLL passed the nine-case descriptor matrix (30/30 assertions, three iterations),
including zero SDL allocation requests over 8,193 warmed compatible-pipeline
draws per iteration. The GPU texture matrix passed 162/162 assertions on each
of D3D12 and Vulkan; all 1,271 exported names and ordinals match graphix.5.
These native results do not establish Cerneala consumer or performance gates.

#### Graphix.6 Windows x64 consumer verification

On September 11, 2026, all three native package references were advanced to
graphix.6; Graphix-CS remains 3.4.16.1. Restored, test-output, Playground and
published smoke DLL hashes match the verified public Windows x64 payload.
The loaded-runtime revision assertion and six-RID restored-asset validator
passed, as did all five dependency-boundary tests.

The complete Release solution build passed with zero warnings and errors.
The first build encountered the previously recorded Visual Studio nested-restore
`NETSDK1047` for LanguageServer's `net10.0/win-x64` assets. The unchanged Visual
Studio test project passed 47/47 independently and regenerated those assets;
the repeated complete build then passed without a source workaround.

The native-enabled solution run passed 5,133 tests across nine projects with
zero failures and four existing NVIDIA alpha-occlusion skips. This includes
641 passing SDL backend tests and all 133 historical Drawing/Prism pixel cases.
The two Windows tests that move the pointer or inject foreground input remained
excluded at the maintainer's request: cursor publication and native
input/graphics ownership. Those input gates remain unverified, not passed.

All six shader artifact checks and the Prism public-surface/completeness audit
passed. Windows x64 and ARM64 smoke publishing and native asset validation
passed; the published Windows x64 multi-window and Prism smoke modes both
completed successfully. ARM64 was published, not executed. Consumer execution
on other RIDs and human runtime validation were not performed. These results
do not establish the separate Scene World performance thresholds.
Commands' output, package checks, TRX results and smoke artifacts are retained
under `.artifacts/graphix-native-6/`.

#### Historical graphix.5 verification

The previous native package records Graphix source commit
`b8784d6580e6c9a7dca1c2c8f9fcfd0ceec4b6eb`, built by
[Graphix CI run 34579034687](https://github.com/Chevalier12/Graphix/actions/runs/34579034687).
All six build/test jobs and package assembly succeeded. Its downloaded NuGet.org
`.nupkg` SHA256 is
`5151837CE8ABB79A22885B9B281E8FAC42BB6E18BE6D0C308B24FCF25F85B433`.
The repository signature and all six RID payloads, provenance records, license
and documentation bytes were verified against Graphix's staged release assets
and committed source. The packaged Windows x64 DLL SHA256 is
`61355686BA270E26B70C1D7E9C298660AF22F97CF03A449855A3C7F5E6B05FB1`.

Graphix.5 adds a Windows input-shutdown phase before generic input state is
released. It stops and joins device hotplug and raw-input producers, including
when joystick keeps a shared notification reference alive. This corrects the
input-lifetime race found during Cerneala test teardown; no managed binding or
public SDL API change is required. Package verification alone does not establish
Cerneala consumer correctness; downstream verification is recorded separately.

#### Graphix.5 Windows x64 consumer verification

On September 11, 2026, the exact public Windows x64 DLL passed all 18 lifetimes
in Graphix's deterministic input-shutdown regression: mouse, keyboard and raw
input, each with full SDL shutdown and video-subsystem shutdown, three iterations
per case. Cerneala's package identity tests passed 3/3, and ten fresh-process
Prism chapter test runs passed 160/160 without a native teardown crash.

The complete Release solution build passed with zero warnings and errors. An
initial build encountered the existing Visual Studio nested-restore
`NETSDK1047`; the unchanged Visual Studio project passed independently and
regenerated the RID assets, after which the full build passed. Three architecture
test expectations still pinned to graphix.4 were updated to graphix.5 without
changing the dependency-boundary assertions; all five boundary tests passed.

The subsequent native-enabled solution run passed 5,107 tests across nine
projects, with zero failures and four existing NVIDIA alpha-occlusion skips.
This includes 632 passing SDL backend tests and all 133 historical Drawing/Prism
pixel-conformance cases. Two Windows tests were excluded to avoid moving the
user's pointer or injecting foreground input: cursor publication and native
input/graphics ownership. Those input gates remain unverified, not passed.
Consumer execution on other RIDs and human validation were not performed.
These correctness results do not establish the separate performance thresholds.

#### Historical graphix.4 verification

The previous native package records Graphix source commit
`7dcfac5a73007e72bd8fb060861e46fe07f55c46`, built by
[Graphix CI run 34256929848](https://github.com/Chevalier12/Graphix/actions/runs/34256929848).
Its downloaded NuGet.org `.nupkg` SHA256 is
`FEEBE8900AAA2CFDFDE48F2941E9CD2DDAA899D36A90375D11DBB4D38BD0C376`.
Its six native CTest suites passed
25/25 each. A separate real Windows-driver maximize regression passed 400/400
assertions on x64 and ARM64. These checks do not certify GPU behavior.
The public packages are independent of Actions artifact retention. NuGet.org adds
a repository signature, so the public ZIP hashes differ from the unsigned CI
artifacts. Every managed/native payload entry was compared byte-for-byte against
the verified CI package, and both NuGet repository signatures were validated.
Never replace a package's payload under an already-used version.

This version retains the independent Windows maximum-dimension correction and
adds D3D12 descriptor-heap and GPU renderer texture corrections. The exact packaged
Windows x64 DLL also passed 21 descriptor assertions and 162 texture assertions
on each of D3D12 and Vulkan; this does not certify GPU execution on other RIDs.
Windows process argument quoting and shell validation also changed: the stricter
batch/cmd argument-list policy is intentionally restrictive, while an explicit
raw command line remains the caller's responsibility. The native header documents
that policy. The independent NVIDIA alpha-occlusion failure is not fixed by this
release. Repository verification and
remaining blockers are tracked in the
[migration audit](audits/2026-09-05-monogame-removal.md). Package CI is not a
substitute for Cerneala's rendering, input, allocation and native-platform gates.

The September 8, 2026 graphix.4 consumer verification ran with native tests enabled:
the SDL project passed 534/538 tests, and the full solution passed 4,802/4,806
executed tests. Both runs retained the same four known NVIDIA alpha-occlusion
failures (content 1, 7, 6, 3; differences 47, 25, 27, 46/255). Native input passed.
The solution also hit the documented Visual Studio nested-restore `NETSDK1047`;
that unchanged test project passed 47/47 in a separate invocation. Neither
recheck nor package publication makes the original full-solution run green.
Windows x64/ARM64 smoke publishing, x64 multi-window and Prism execution, six
shader artifact checks and the Prism public-surface audit passed. Consumer
execution on Linux, macOS and Windows ARM64, and human validation, were not run.
Local commands, logs and TRX results are retained under `.artifacts/graphix-native-4/`.

### Published application assets

| Runtime identifier | Required SDL3 asset | Expected SDL_GPU driver |
| --- | --- | --- |
| `win-x64`, `win-arm64` | `SDL3.dll` | D3D12 |
| `linux-x64`, `linux-arm64` | `libSDL3.so`, with its versioned links/files | Vulkan |
| `osx-x64`, `osx-arm64` | `libSDL3.0.dylib` and `libSDL3.dylib` | Metal |

Use the repository publish helper to build and validate an output directory without deleting existing artifacts:

```powershell
.\Tools\scripts\Publish-SdlGpuSmoke.ps1 `
  -RuntimeIdentifier win-x64 `
  -OutputRoot artifacts\sdlgpu-publish
```

The script fails if the expected SDL3 asset is absent or an SDL native asset for another operating-system family leaked into the output.

Run the published application through the matching launch helper:

```powershell
.\Tools\scripts\Invoke-SdlGpuSmoke.ps1 `
  -RuntimeIdentifier win-x64 `
  -PublishedDirectory artifacts\sdlgpu-publish\win-x64 `
  -Mode multi-window `
  -ArtifactDirectory artifacts\sdlgpu-smoke
```

Available smoke modes cover single-window, multi-window, input, resize, Drawing, `RenderSurface2D`, Prism, and screenshot behavior. Screenshots are captured only through `Window.SaveScreenshot`; no operating-system screen-copy API is used.

## CI notes

The desktop workflow publishes both architectures for each operating-system family and executes native multi-window and Prism smoke tests on Windows, Linux, and macOS. Linux CI uses Xvfb and Mesa lavapipe when no physical display/GPU is available. This software Vulkan configuration is a CI fallback, not a runtime requirement for user applications.

The same Windows x64, Linux x64/Vulkan, and macOS arm64/Metal matrix also builds the SDL and core test projects, enables `CERNEALA_SDL_NATIVE_TESTS=1`, and executes the real offline-artifact pipeline creation test plus the current Drawing/Prism pixel-conformance corpus. The job validates exact TRX counts (1 pipeline test and 133 conformance cases), rejects any non-executed mandatory test, and uploads the `Window.SaveScreenshot` images, references, heatmaps, and diff reports. Linux runs these tests under `xvfb-run` after selecting the lavapipe ICD. Cross-published secondary RIDs are packaging evidence only and do not substitute for those three native executions.

## See also

- [Application markup and backend selection](application-markup.md)
- `Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend`
- `Cerneala.UI.Hosting.Windowing.ApplicationBackendAttribute`
