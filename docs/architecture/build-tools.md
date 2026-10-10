# Build Tools

> Code: `Tools/`, `Cerneala.Backends.SdlGpu/Cerneala.Backends.SdlGpu.csproj` (shader targets), `.github/workflows/` · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

The tools under `Tools/` produce or check files that the repository commits:

- shader binaries;
- SVG raster sidecars;
- Scene2D packages;
- the Prism completeness report;
- the Prism filter reference.

They run at development time or in CI, never inside a published application. The Roslyn navigation server is a developer tool and is only linked here.

## Components

| Tool | Path | Output | Run by |
|---|---|---|---|
| SDL shader compiler | `Tools/Cerneala.SdlShaderCompiler` | `.spv`, `.dxil`, `.msl` per shader, plus `Shaders/artifacts.json` | a developer (generate); MSBuild and CI (`--verify`) |
| `VerifySdlShaderArtifacts` | MSBuild target in `Cerneala.Backends.SdlGpu.csproj` | a stamp file | every build of `Cerneala.Backends.SdlGpu` |
| SVG asset compiler | `Tools/Cerneala.SvgAssetCompiler` | `<name>.cerneala.png` plus `<name>.cerneala.png.sha256` | a developer, by hand |
| Scene2D package compiler | `Tools/Cerneala.Scene2D.PackageCompiler` | a package directory | a developer; `Playground` build targets. See [scene2d.md](scene2d.md) |
| PrismAudit | `Tools/PrismAudit` | `docs/reference/prism-completeness-report.generated.md` | a developer (`--write`); CI (`--check`) |
| Prism filter reference | `Tools/scripts/New-PrismFilterReference.ps1` | `docs/reference/prism-filter-reference.generated.md` | a developer, by hand |
| Roslyn MCP | `Tools/RoslynMcp`, `Tools/scripts/Install-RoslynMcp.ps1`, `Start-RoslynMcp.ps1` | a local `mcpRoslyn.exe` install | a developer; see [Tools/RoslynMcp/README.md](../../Tools/RoslynMcp/README.md) |

The solution `Cerneala.slnx` contains PrismAudit and the shader, SVG and Scene2D compilers. `Tools/TimbreDecoderProbe` and `Tools/TimbreDecoderSelection` are Timbre stage-0 helpers outside the solution. `Tools/scripts/` also holds the SDL smoke publish/run scripts, `Test-MarkdownLinks.ps1` and other harness scripts.

## Data Flow

### Shader compiler

Pinned packages in `Cerneala.SdlShaderCompiler.csproj`, all `PrivateAssets="all"`:

| Package | Version |
|---|---|
| `Graphix-CS` | `3.4.16.1` |
| `Graphix.Native` | `3.4.16-graphix.6` |
| `SDL3-CS.Windows.Shadercross`, `SDL3-CS.Linux.Shadercross`, `SDL3-CS.MacOS.Shadercross` | `3.0.0.9` |

`Program.cs` also declares `ToolVersion = "2"` and `ShaderCrossVersion = "3.0.0"`, and the manifest must declare the same versions.

The command line has two optional arguments:

```powershell
# Generate or update the committed artifacts:
dotnet run --project Tools/Cerneala.SdlShaderCompiler -- --manifest Cerneala.Backends.SdlGpu/Shaders/manifest.json
# Recompile in memory and compare, writing nothing:
dotnet run --project Tools/Cerneala.SdlShaderCompiler -- --verify
```

- Without `--manifest`, the path `Cerneala.Backends.SdlGpu/Shaders/manifest.json` is used, relative to the current directory.
- Any other argument throws `Unknown or incomplete argument '...'`.
- Every exception prints its message and returns exit code `1`. Success returns `0`.

For each shader in the manifest, the compiler:

1. compiles HLSL to SPIR-V and to DXIL;
2. transpiles SPIR-V to MSL;
3. reflects the SPIR-V and validates the portable limits, the interface layout and the bindings.

The HLSL inputs are the shader's own source plus every `*.hlsl` under the manifest's common roots (`Drawing/Prism/Shaders/Hlsl`).

`artifacts.json` records:

- the tool and ShaderCross versions;
- a manifest hash and a source hash;
- a SHA-256 for each output.

Text artifacts (`.msl` and the metadata) are compared after their line endings are changed to `\n`. `.spv` and `.dxil` are compared byte for byte. In generate mode, a file is written only when it differs. In verify mode, a missing or different file throws `SDL shader artifact '<path>' is missing or stale.`

The published application loads the committed artifacts as embedded resources: 10 shaders × 3 formats = 30 `EmbeddedResource` items. It never compiles shaders. See [sdl-desktop-backend.md](sdl-desktop-backend.md).

### `VerifySdlShaderArtifacts`

`Cerneala.Backends.SdlGpu.csproj` declares three targets. None runs in a design-time build.

1. `RestoreSdlShaderCompiler` runs `dotnet restore` for the compiler when its `.csproj` is newer than its `project.assets.json`.
2. `VerifySdlShaderArtifacts` runs `BeforeTargets="PrepareResources"`, after `RestoreSdlShaderCompiler`:
   - `Inputs="@(SdlShaderInput)"`: the manifest, `Gpu/Shaders/*.hlsl`, `Prism/Shaders/*.hlsl`, `Drawing/Prism/Shaders/Hlsl/**/*.hlsl`, the compiler `.csproj` and its `Program.cs`;
   - `Outputs`: `obj/<Configuration>/<TargetFramework>/sdl-shaders.verify.stamp`;
   - with no artifact files at all, it fails with `No versioned SDL_GPU shader artifacts were found.`;
   - otherwise it runs the compiler with `--verify --manifest <manifest>` and touches the stamp.
3. `RequireSdlShaderArtifacts` also runs before `PrepareResources`. It fails when an artifact file or `artifacts.json` does not exist.

Example: you edit a file under `Drawing/Prism/Shaders/Hlsl/` and build without regenerating:

1. The edited file is newer than the stamp, so the verify target runs.
2. The recompiled bytes differ from the committed `.spv`, so the build fails with `SDL shader artifact '...' is missing or stale.`
3. Run the generate command above and commit the changed artifacts and `artifacts.json`.

### SVG asset compiler

```powershell
dotnet run --project Tools/Cerneala.SvgAssetCompiler -- logo.svg logo.svg.cerneala.png
```

1. Checks the arguments. Two are required; otherwise it prints the usage and returns `2`. A missing input also returns `2`.
2. Rasterizes the SVG with `SvgRasterizer.Compile`.
3. Writes the PNG and `<output>.sha256`, the SHA-256 of the source bytes. It writes `.tmp` files first and then moves them into place.
4. Returns `0`.

At runtime, `SvgRasterizer.Acquire(path)` looks for `path + ".cerneala.png"` and its `.sha256` file. It is used by `SvgImage` and the SDL image loader. It reads the PNG only when the stored signature equals the SHA-256 of the current SVG; otherwise it rasterizes the SVG itself. Example: `logo.svg` changes after compiling, so its hash no longer matches and the stale `logo.svg.cerneala.png` is ignored.

### PrismAudit

```powershell
dotnet run --project Tools/PrismAudit -c Release -- --check
dotnet run --project Tools/PrismAudit -c Release -- --write
```

- No flag means `--check`. Both flags together throw.
- The audit checks the Prism catalog (`Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`):
  - entry counts per kind: 134 filters, 10 styles, 28 blend modes, 5 color profiles, 1 sampling;
  - the common properties and their defaults;
  - the directives;
  - the public Prism API, through reflection;
  - the required design tokens in [prism-technical-design.md](prism-technical-design.md);
  - the `catalog-sha256` line of the filter reference.
- Any gap prints `Prism completeness audit failed with N gap(s):` and returns `1`.
- `--check` then compares the stored report, with `\n` line endings, to the regenerated text. A difference prints `Prism completeness report is stale. Run: dotnet run --project Tools/PrismAudit -- --write` and returns `1`.
- `--write` writes the report and returns `0`.

The report contains the hashes of its design inputs, so editing `prism-technical-design.md` makes `--check` fail until `--write` runs again. The project targets `net8.0-windows` and references `Cerneala.Backends.SdlGpu`. Building it therefore also runs the shader targets above; this follows from the project reference and was not run separately.

### Prism filter reference

```powershell
pwsh -NoProfile -File Tools/scripts/New-PrismFilterReference.ps1
```

The script:

- reads `prism-catalog.json`;
- writes one section per filter, sorted by `stableId`, plus the line `<!-- catalog-sha256: <hash> -->`;
- writes UTF-8 without a BOM.

`-OutputPath` changes the output file; a relative path is resolved from the repository root. The hash is computed over the raw catalog bytes. PrismAudit hashes the catalog text after normalizing line endings to `\n`, so the two hashes are equal only while the catalog file has no CR bytes. `.gitattributes` checks the catalog JSON out with `eol=lf`.

## Lifecycle And Ownership

- Generated outputs are committed: the shader artifacts and `artifacts.json` under `Cerneala.Backends.SdlGpu/{Gpu,Prism}/Shaders`, the `*.generated.md` files under `docs/reference`, and the SVG sidecars next to their project, for example in `CernealaPresentation` and `Tetrisish`. Each one has exactly one generator; editing it by hand makes its check fail, or is overwritten by the next run.
- The tools hold no state between runs. The shader verify stamp in `obj/` is the only build-time cache.
- Roslyn MCP installs outside the repository, under `%LOCALAPPDATA%\CernealaTools\mcpRoslyn\`.

## Frame Integration

None. The tools run at build or development time, not in the frame loop.

## Invariants

- `VerifySdlShaderArtifacts` runs before `PrepareResources`, tracks the manifest, every HLSL root, the compiler project and its `Program.cs`, runs `--verify --manifest` and touches its stamp. `tests/Cerneala.Tests/Drawing/Prism/PrismShaderBuildIncrementalityTests.cs:ShaderVerificationTracksSharedStylesWrappersAndCompilerInputs`. This test reads the `.csproj` XML; it does not run a build.
- The shader compiler uses `Graphix-CS 3.4.16.1` and `Graphix.Native 3.4.16-graphix.6`, and no plain `SDL3-CS` package. `tests/Cerneala.Tests/Architecture/SdlDependencyBoundaryTests.cs:ShaderCompilerUsesTheSameGraphixManagedAndNativePackages`.
- A sidecar whose signature matches is returned byte for byte; a sidecar with a wrong signature is ignored, and the 7 × 5 SVG is rasterized instead of the 3 × 2 PNG being used. `tests/Cerneala.Tests/Controls/SvgImageTests.cs:SvgRasterizerUsesACompiledSidecarWithoutParsingTheSourceAtRuntime`, `SvgImageTests.cs:SvgRasterizerRejectsACompiledSidecarForDifferentSourceContent`.
- The committed artifacts match the current sources: checked by CI, not by a unit test. `.github/workflows/prism-shaders.yml` and `desktop-backends.yml` run the compiler with `--verify`. `desktop-backends.yml` also runs `PrismAudit --check`.
- No test runs the shader compiler, PrismAudit, the SVG compiler's `Program` or `New-PrismFilterReference.ps1`; their exit codes come from the source: netestat.

## Known Limitations

- A hand-edited `.spv`, `.dxil` or `.msl` is not an input of `VerifySdlShaderArtifacts`. After the first verification, the build skips the target until an input or the stamp changes. CI's explicit `--verify` steps still catch it. This follows from the MSBuild `Inputs`/`Outputs` rules and was not run.
- `RequireSdlShaderArtifacts` reports `missing or stale`, but it checks only that the files exist.
- Nothing runs the SVG compiler or `New-PrismFilterReference.ps1` automatically. PrismAudit checks only the hash line of the filter reference, not the rest of its text.
- PrismAudit targets `net8.0-windows`, so `--check` runs only on Windows.
- `docs/audits/2026-08-26-sdlgpu-stage-6-shader-toolchain.md` states that the compiler pins SDL3-CS 3.4.14.1. That is historical; the current pins are in the table above.
