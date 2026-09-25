# Scene Village

A separate Windows SDL_GPU desktop game built on the current public Scene2D
surface. Run it with:

```powershell
dotnet run --project .\Playground\Cerneala.SceneVillage\Cerneala.SceneVillage.csproj -c Release
```

In the current workspace, pre-existing `artifacts/rendersurface3d/.../obj/*.cs`
can be included by the Core SDK glob and break a normal build before this
game's source is compiled. Keep those artifacts intact. Build Release with
the existing artifact exclusion, then launch that build without rebuilding:

```powershell
$env:DefaultItemExcludesInProjectFolder = 'artifacts/**'
dotnet build .\Playground\Cerneala.SceneVillage\Cerneala.SceneVillage.csproj -c Release '-p:DefaultItemExcludesInProjectFolder=artifacts/**'
dotnet run --project .\Playground\Cerneala.SceneVillage\Cerneala.SceneVillage.csproj -c Release --no-build
```

Use **WASD** for continuous movement and **R** to return to the village
spawn. Click a workload button to select static decor, animated decor or
collidable objects, then click **0**, **100**, **1k** or **10k** for the requested
stress count. The bottom status shows **requested / realized stress objects**;
the fixed village scenery and player are not included in either number.
**Stress field** teleports the player to a safe inspection waypoint amid the
stress grid; **Village** (or R) returns to the village spawn. These buttons
are navigation shortcuts, not simulated walking.

The player has an active collider and cannot pass through houses and trees.
Each large green tree uses the Tiny Town atlas's vertically adjoining cells
4 and 16 as one 16 x 32 source image. Its 16 x 32 world sprite extends above
the lower-cell site; the trunk collision box scales with the artwork. The
rounded foliage from cell 5 is shown as supplied by the source sheet.
Diagonal movement is normalized. Movement is delta-time based, while the
camera immediately follows the player with a finite-world edge clamp. The
camera changes `RenderSurface2D.ViewBox`; it does not translate the collision
world. Tiny Town's 16-pixel cells occupy 16 world units; each 32-pixel player
or stress-villager frame occupies 32 world units. The world remains 4096 units
wide, and the denser ground and paths retain their previous world-space
coverage. Stress locations keep their 32-unit grid pitch.

`VillageGameSurface.Zoom` is a programmatic camera control (default `1`);
positive finite values change the followed ViewBox, not the renderer target.
Its nullable `ContentScale` defaults to the window DPI scale when unset. This
game sets `ContentScale="1"` in its window markup, so at Zoom 1 it requests
one full-resolution target pixel per world unit regardless of window DPI.
Other UI retains ordinary DPI scaling. The camera uses the surface's rounded
physical target extent when it computes the ViewBox; it does not render a
low-resolution target and enlarge it. No zoom key, wheel gesture or toolbar
control is currently wired.

Stress locations are a deterministic shuffled grid around the village. All
three presets use the same ordered locations, sprite sheet, dimensions and
count. The **static** and **collision** presets show the same idle villager
frame; collision adds stationary box colliders. The **animated** preset uses
the actual four-frame down walk cycle from the licensed villager sheet,
looping in place; those sprites are not autonomous NPCs. The player uses the
same sheet's down, up and right rows, and mirrors the right frames to face
left through `SpriteAnimationFrame.Flip`. Both player and stress villagers
use Point sampling for the tightly packed pixel-art sheet, as do Tiny Town
sprites; the framework's default Sprite2D sampling remains Linear. The
central village remains
clear in every preset so the spawn and walkable paths are not blocked.
Switching a preset replaces the real `SceneItems2D.ItemsSource`. The app does
not filter offscreen items or pretend that 10,000 realized items means 10,000
visible draw calls. Rendering and presentation culling remain Cerneala's
responsibility. There is no app-reported FPS, allocation or GPU performance
claim; use Cerneala's existing frame diagnostics for a measured investigation.

Artwork and exact source/archive hashes are in [Assets/CREDITS.md](Assets/CREDITS.md).
The art is CC0, and the original Kenney license text is included.

The dedicated tests are in `tests/Cerneala.Tests.SceneVillage`. The native
Windows SDL case is opt-in with `CERNEALA_SDL_NATIVE_TESTS=1`; it uses Servo
for user-like input and `Window.SaveScreenshot` for app-owned captures.
The deterministic state test proves the held-key clearing method used by
the app's `Deactivated` callback stops movement. Physical Alt-Tab / OS focus switching is **not** automated
by the current native harness and remains a human validation step. No human
validation is claimed here.
