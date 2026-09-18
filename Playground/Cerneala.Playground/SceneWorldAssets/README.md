# Scene World assets and prepared builds

Edit `village.tmj` in Tiled or `village.ldtk` in LDtk. `world-atlas.png` is their
shared source atlas. `New-Assets.ps1` regenerates the authored sample fixtures;
it is not a runtime loader.

Build the Playground normally:

```powershell
dotnet build Playground/Cerneala.Playground/Cerneala.Playground.csproj -c Release
```

The project builds the existing preparation tool as a build-only dependency and
generates `tiled` and `ldtk` packages under its dedicated
`obj/SceneWorldPackages/<configuration>/<target-framework>` subtree. Only these
two generated directories may be replaced by the target. The CLI's general
create-only policy is unchanged. Changes to authored maps, atlas, preparation
assemblies or the target invalidate preparation. Design-time builds do not run
the compiler.

Build and publish outputs contain `SceneWorldPackages/tiled` and
`SceneWorldPackages/ldtk`, each with its catalog, indexed payload file and atlas.
Distribute each directory as a unit. The running demo needs the optional package
module, not the importer or package compiler. Raw editor documents are not
application content; the test project separately copies them as comparison
fixtures under `SceneWorldAuthoring`.

The demo keeps package catalogs and gameplay state, not complete editor models.
The door suppression and Plant toggle are application-owned cell deltas. Plant
adds/removes the decorative flower at Buildings cell (11,10), beside the authored
flower at (10,10); it never replaces the Terrain grass. Its asynchronous command
prepares the edited chunk and its used palette before publishing on the UI relay.
Current scene interests acquire that prepared payload during publication, so a
local Plant edit does not put the visible scene into Loading. Staging releases in
all completion/cancellation paths and is not a permanent backing map: later
acquisitions reconstruct the payload from the unchanged package. Edited payloads
have an unknown data charge and are not eligible for optional warm residency.
This sample is not a general editing API or a persistent save system; the
framework's whole-scene Loading policy is unchanged.

Reset prepares the spawn region before changing the player position and does
not undo Plant. Changing format discards local Plant edits and restores authored
player/door state, while retaining the NPC source and active actor nodes. Leaving
the view cancels pending work, releases collision interests and closes the
package. Reattaching the same view reopens it and reapplies its gameplay delta.
Commands received while required operation data is unavailable are not queued
for later execution. Loading failures are shown in the status instead of being
treated as empty terrain.

The opt-in maintained native scenario uses
`CERNEALA_SCENE_WORLD_CAPTURE=<absolute-artifact-directory>`. It selects Scene
World, drives buttons and keys through Servo, captures through the application's
Window screenshot path, writes `results.json` or `failure.txt`, and closes the
window. Three Plant clicks per format also check every intervening presentation
state, committed-root validity and single-batch rebuilding through Detective;
settled screenshots use window-backed Servo. The scenario also adds 15 NPCs
(16 total) and performs 32 consecutive Pan clicks per format, covering repeated
camera exit/re-entry while preserving active actor count and identity. These
captures redraw retained
commands, not OS scanout. This is automated validation, not a claim of human
manual validation.
