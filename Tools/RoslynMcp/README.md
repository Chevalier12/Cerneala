# Local Roslyn navigation for Codex

This integration provides structured C# symbol navigation over saved files in
`Cerneala.slnx`. It is not natural-language retrieval: there are no embeddings,
model weights, GPU inference, vector databases, HTTP listeners, or cloud query
credentials. It runs an independent Roslyn/MSBuild workspace, not the editor's
language-server process or its unsaved buffers.

## Ownership and maintenance

The implementation is [mcpRoslyn](https://github.com/MBrekhof/mcpRoslyn), MIT
licensed, pinned to v1.4.0 commit
`dfccb63bfac1c82274ffc0b59a41684d5ca3b900`. `manifest.json` owns the pin,
variant identity and Codex tool allow-list. `roslyn-5.9.patch` changes only the
two compiler/workspace package references from 5.3.0 to 5.9.0. There are no
Cerneala framework, generator or `.crn` language-server changes.

The 5.3 host rejected Cerneala.SourceGen (compiler 5.6) and SDL3-CS.Generators
(compiler 5.9) with `ReferencesNewerCompiler`. A permanent real-repository
regression checks `PresentationWindow.NextButton`, generated from markup.
The synthetic freshness fixture uses a 5.9 incremental generator as well.

Upstream upgrades require source inspection, reapplying the bounded patch,
running upstream tests and the integration regression, and reviewing tool
schemas/read-only paths before changing the allow-list. Do not silently
follow the upstream default branch or loosen tests to accept missing symbols.

## Install (Windows x64)

Requirements: Git, PowerShell 7, and the .NET SDK selected by this repo's
`global.json`. From the repository root:

```powershell
pwsh -NoProfile -File ./Tools/scripts/Install-RoslynMcp.ps1
# Source generators must already have build outputs for MSBuildWorkspace to load.
dotnet build ./CernealaPresentation/CernealaPresentation.csproj -c Debug
```

Installation downloads source and restores NuGet packages. It verifies the
commit, applies the checked-in patch and publishes outside the repository at:

```text
%LOCALAPPDATA%\CernealaTools\mcpRoslyn\<commit>-<variant>\
    source\
    publish\mcpRoslyn.exe
    publish\BuildHost-netcore\
    publish\BuildHost-net472\
    LICENSE
    installation.json
```

Do not remove BuildHost sidecars. The installer is create-only for completed
installations and refuses unexpected source changes or an unreceipted publish
directory; it never resets an existing checkout or deletes an installation.
Failed publish staging directories are retained for inspection.

`Start-RoslynMcp.ps1` verifies the pin/patch/executable receipt and starts the
published executable through native PowerShell invocation with UTF-8 stdio.
It imports the current host's utility module explicitly, so inherited PS7
module paths cannot shadow the PS5 hash function. It performs no download, restore,
build or stdout logging. Diagnostics go to stderr. The SDK is still needed
for MSBuild solution evaluation even though the executable is self-contained.

## Codex connection

The project entry is `[mcp_servers.cerneala_roslyn]` in `.codex/config.toml`.
Its launcher and cwd are absolute paths for this workstation; update those
two paths after moving the checkout. No user/global config is modified.
The project must be trusted. After changing configuration, use the app's MCP
**Restart** action; a successful standalone protocol test does not prove the
tools have loaded into an already-running Codex chat. See the
[official OpenAI MCP documentation](https://learn.chatgpt.com/docs/extend/mcp).

The allow-list contains:

- `workspace_symbol`, `list_document_symbols`, `hover`, `goto_definition`
- `find_references`, `find_implementations`, `find_derived_types`
- `find_callers`, `find_callees`, `analyze_symbol`
- `project_overview`, `reload_workspace`

`rename_symbol` is explicitly denied. Diagnostics/analyzer execution tools,
heuristic dead-code/test/registration tools, indexed semantic-search tools and
echo are not exposed. The bare upstream executable still advertises them:
**read-only is a Codex tool-selection boundary, not a sandbox**. Loading a
trusted project can execute MSBuild logic and source generators; do not point
this server at untrusted repositories. `reload_workspace` can switch solutions.

## Navigation contract

1. Use `workspace_symbol` for name/pattern discovery, then use its exact opaque
   `symbolId` or a source position. Do not fabricate IDs or identify overloads
   by text matches alone. Check the search result's `truncated` flag.
2. Positions are **1-based** line and column. On ambiguous cross-assembly IDs,
   select an actual declaration position. Linked-file positions select the
   first loaded project; there is no target-framework selector. IDs do not
   fully distinguish target-framework variants of one assembly.
3. `find_implementations` handles interface implementations and concrete
   class-member overrides. `find_derived_types` defaults to direct descendants;
   explicitly request `transitive=true` for the full static type hierarchy.
4. Callers/callees are static symbol relationships, not runtime execution.
   Callees collect explicit invocation syntax, deduplicate methods and default
   to a cap of 50 without a truncation flag. They do not establish all
   constructor/accessor/implicit/reflection/dynamic/delegate/virtual-dispatch
   edges. `analyze_symbol` has callers but no callees section; inspect its
   section counts and `truncated` list. Reproduce runtime behavior separately.
5. Error-tolerant positional binding can choose a candidate symbol at a broken
   compilation site. Inspect warnings/errors and verify the intended signature
   before treating that binding as decisive evidence.
6. Generated definition locations can be virtual `.g.cs` paths rather than
   disk files. Do not assume a returned path exists. Trace the generator owner
   when source-generation behavior is part of the investigation.

### Freshness

- Save editor buffers first. Existing ordinary `.cs` files refresh on tool
  calls; this is not an unsaved-buffer overlay.
- Call `reload_workspace` after adding/removing/renaming files or changing
  projects, solution/build configuration, analyzer outputs, or dependencies.
- **Call `reload_workspace` after changing `.crn` or any AdditionalFile.**
  AdditionalFiles are not refreshed per call and may not emit stale warnings.
  Do not wait for a warning before reloading those changes.
- Concurrent reloads or other stale warnings invalidate confidence in a result;
  reload/reissue the query against the intended saved snapshot.
- `project_overview` defaults to 25 projects and caps reference/package lists.
  Increase its limits to cover this solution. Document counts exclude generated
  documents; package values are literal `.csproj` entries, not a complete
  evaluated dependency graph.

## Verification

The integration harness requires Python 3.11+ with only the standard library.
Codex's bundled Python can be used if `python` is a Windows Store alias.
Use a new, nonexistent evidence directory on each run:

```powershell
$manifest = Get-Content -Raw ./Tools/RoslynMcp/manifest.json | ConvertFrom-Json
$exe = Join-Path $env:LOCALAPPDATA "CernealaTools/mcpRoslyn/$($manifest.commit)-$($manifest.variant)/publish/mcpRoslyn.exe"
python ./Tools/RoslynMcp/test_roslyn_mcp.py --exe $exe --output ./.artifacts/roslyn-mcp/my-run
python ./Tools/RoslynMcp/test_roslyn_mcp.py --launcher ./Tools/scripts/Start-RoslynMcp.ps1 --output ./.artifacts/roslyn-mcp/my-launch-run --generated-only
```

`--generated-only` is the focused regression, not full verification. Full mode
also checks overloads, private identity, cross-project implementations,
inheritance/base calls, composite analysis, project references, invalid
positions, saved C# edits, new files after reload, and generated members after
an AdditionalFile reload. It creates and removes only its own unique external
temporary solution; it never edits real repository source. Raw JSONL protocol,
stderr, timings, process memory and failure evidence remain under the requested
output directory. Main-process memory measurements exclude child BuildHosts;
launcher per-request counters measure only the launcher process. A separate
single process-tree snapshot includes child BuildHosts; its working-set sum can
double-count shared pages, and private bytes include nonresident commit.

The installer retry comparison has its own read-only regression against the
patched checkout. It accepts relocated patch hunks but rejects changed content:

```powershell
$source = Join-Path $env:LOCALAPPDATA "CernealaTools/mcpRoslyn/$($manifest.commit)-$($manifest.variant)/source"
pwsh -NoProfile -File ./Tools/RoslynMcp/test_install_retry.ps1 -SourcePath $source
```

Upstream non-explicit tests belong to the patched external checkout; run them
serially because tests mutate their copied fixtures. Manual benchmark tests
target another real repository and are intentionally not enabled here.

The initial full solution load and first generator/reference queries can cost
tens of seconds. A repeated warmed query is not a cold-start benchmark. Keep a
single workspace connection where possible; do not assume extra parallel
connections are free in RAM. GPU acceleration is not part of these operations.

### Verified on this workstation (2026-10-02)

- The permanent generated-member regression failed on the original 5.3 host
  with `SYMBOL_NOT_FOUND`, and passed on the pinned 5.9 variant.
- Full direct-executable and full configured-launcher integration runs passed,
  including saved C# changes, explicit new-file reload and AdditionalFile
  generator reload. Missing/mismatched installation guards passed with empty
  protocol stdout. All test-owned processes exited and the successful temporary
  fixtures were removed after exit.
- Upstream: **227 passed, 0 failed**; three explicit manual benchmarks were not
  executed. Its build reported 10 NUnit analyzer warnings, no errors.
- Project TOML and `codex mcp get cerneala_roslyn --json` agreed with the
  12-tool manifest/rename denial. This checks configuration parsing, not actual
  desktop-chat activation or live Codex allow-list enforcement.
- After the user's Codex restart, live MCP calls verified symbol search and
  distinct overload IDs, generated `NextButton` hover/definition/references,
  and `TextBlock.GetTextMeasurer` call sites. The chat catalog exposed exactly
  the 12 configured tools and did not expose `rename_symbol`.
- Launcher full-run initialize: **29.021 s**; first generated hover/definition/
  references: **24.612 / 6.595 / 25.892 s**; repeated ordinary symbol query:
  **0.826 s**. These are single-run samples, not P95/P99 or acceptance limits.
- One snapshot after the initial generated-reference query: **1,337.6 MiB**
  summed process-tree working set, **1,166.1 MiB** summed private bytes. It
  contained PowerShell, conhost and mcpRoslyn; no live BuildHost child appeared
  in that snapshot. Working-set sums may double-count shared pages; private
  bytes are committed memory, not necessarily resident. A separate direct full
  run reached **1,238.8 MiB** main-process peak working set across reloads,
  excluding child processes. Neither measurement is a guaranteed memory cap.

Raw evidence is retained under `.artifacts/roslyn-mcp/`: `regression-red-5.3`,
`green-5.9-full-v3`, `launcher-5.9-full-v3`, `upstream-tests`, and
`codex-config.json`. Earlier failed harness/launcher runs are retained too, not
counted as passing gates. The launcher fixes were checked against protocol RED:
host-specific utility-module resolution and actual forwarding of redirected
stdio, rather than increasing timeouts to hide missing responses.

The build-input exclusion checks passed. The full Cerneala framework/UI suite
was not run for this external-tool/config/documentation-only change; no runtime
UI, renderer or public framework API was modified. Live use in the current
Codex chat was verified after the user's restart; future sessions must still
check actual tool availability rather than assume activation.

Two old external scratch directories remain inactive because recursive cleanup
was denied by execution policy: the original 5.3 installation's `probe` and
`%LOCALAPPDATA%\Temp\cerneala-roslyn-mcp-4c5zeh6o`. They are not required by
the integration. Successful later runs cleaned up their own fixtures normally.
