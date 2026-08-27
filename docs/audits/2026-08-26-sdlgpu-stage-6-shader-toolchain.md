# SDL_GPU stage 6 shader-toolchain audit

Date: 2026-08-26

## Scope and pinned toolchain

- `Tools/Cerneala.SdlShaderCompiler` targets `net8.0` and pins SDL3-CS desktop packages at `3.4.14.1` and all three desktop ShaderCross packages at `3.0.0.9` with `PrivateAssets="all"`.
- The compiler records tool schema version `2`, SDL_shadercross upstream version `3.0.0`, and package version `3.0.0.9`.
- `Cerneala.Backends.SdlGpu` has no ShaderCross package reference. Inspection of its Release output returned `NO_SHADERCROSS_RUNTIME_ASSETS`.

## Common HLSL and backend wrappers

The Prism math and kernels now have one implementation under `Drawing/Prism/Shaders/Hlsl/`. MonoGame retains only its `.fx` entry wrappers, parameter declarations, MGFX techniques/passes, and the two include-order adapters `Common/AllBlends.fx` and `Common/AllColor.fx`. SDL_GPU has separate drawing and Prism entry wrappers.

The seven WindowsDX/MGFX artifacts were compared byte-for-byte with the hashes captured before extraction:

| Artifact | SHA-256 before and after extraction |
|---|---|
| `Charcoal.mgfxo` | `6950CDF5709B4D12BF24F3C86B3671520F46155473CD00B002BB902292870E71` |
| `ConteCrayon.mgfxo` | `0C0529CAA5BCA54A36A7333EDC8DECAE797A57E78641B0E97D8CF02D00D432AF` |
| `CopyComposite.mgfxo` | `E9585CEBABFCCDBBE2EF0DB3D410276528985027A46A770753CC4743810F7672` |
| `Deinterlace.mgfxo` | `38A9C808CE3B752FFCFBC91F668E18772E7D48AD0C9FD3D4761373AC7125269A` |
| `GraphicPen.mgfxo` | `A7F41FA029B4BDEBC1D6A031106EF30272BC948E4F4911AC995C8EB47671CA73` |
| `Plaster.mgfxo` | `83A242C66FCCA306B2B03B01B3329D4EAC279904D6C305E1BCE7EC255AFEE8D5` |
| `Styles.mgfxo` | `2F1BAC305A7A6D1F6A7CC4A6B77233116811F8A7E7D8EB6E95EB7F8FA7A13700` |

The current Release build of `Cerneala.Backends.MonoGame` completed with zero warnings and zero errors.

## Manifest, reflection, and artifacts

`Cerneala.Backends.SdlGpu/Shaders/manifest.json` declares four logical shaders:

| Logical shader | Stage | Variants | Samplers | Storage textures/buffers | Uniform buffers | Vertex inputs | Reflected inputs/outputs |
|---|---|---|---:|---:|---:|---:|---:|
| `drawing-vertex` | Vertex | `default` | 0 | 0 / 0 | 1 | 3 | 3 / 2 |
| `drawing-fragment` | Fragment | `textured`, `untextured` | 1 | 0 / 0 | 0 | 0 | 2 / 1 |
| `prism-fullscreen-vertex` | Vertex | `fullscreen` | 0 | 0 / 0 | 1 | 3 | 3 / 2 |
| `prism-copy-fragment` | Fragment | `premultiplied-alpha` | 1 | 0 / 0 | 1 | 0 | 2 / 1 |

The manifest records named, zero-based uniform/sampler/storage slots and vertex semantic/location/format entries. ShaderCross reflection must exactly match the declared resource counts and the portable SDL_GPU limits before any output is accepted.

Each shader is generated as SPIR-V, DXIL, and MSL. `Shaders/artifacts.json` records the tool/upstream/package versions, manifest hash, aggregate source hash, interface layout, and SHA-256 for every generated output. SDL runtime selection is deterministic (`DXIL`, then `SPIR-V`, then `MSL`) and fail-closed if the device exposes none of those formats.

## Determinism and build integration

- A normal compiler run generated all four definitions and their twelve format artifacts.
- `--verify` subsequently verified all four definitions without writing.
- A copied fixture with a deliberately replaced SPIR-V artifact was rejected as `missing or stale`.
- A tracked wrapper source was temporarily changed; the backend build rejected stale `artifacts.json`, and passed again after the source was restored.
- The first Release backend build ran `Verified 4 SDL shader artifacts`; an immediately repeated build omitted that target, proving the `Inputs`/`Outputs` incremental stamp was effective.
- Missing artifacts and metadata are rejected by the unconditional fail-closed resource target. Shader compilation never occurs in the shipped backend.

## Runtime verification

- SDL test suite: 42 passed, 4 native tests skipped by default, 0 failed.
- Windows native SDL test filter: 8 passed, 0 skipped, 0 failed.
- The format test loaded every logical shader from each embedded DXIL, SPIR-V, and MSL artifact through the SDL abstraction.
- The native test created all four shaders and real drawing and Prism graphics pipelines on the available Windows SDL_GPU driver.
- `Cerneala.Backends.SdlGpu` Release build: zero warnings, zero errors.

Linux and macOS packages, sources, artifact formats, and runtime selection remain implemented. Native compilation/driver smoke execution on Linux/Vulkan and macOS/Metal is explicitly waived for this run at the user's request because no such runners are available; it was not removed from the implementation.
