# Cerneala Tetris

A desktop Tetris game rendered by Cerneala `RenderSurface2D` through the SDL3 + SDL_GPU backend.

Each tetromino has its own piece class. The active piece uses Prism
`BevelEmboss`, a pulsing `OuterGlow`, and a `MotionBlur` that ramps while Down is
held. The active `Sprite2D` owns these effects in `MainWindow.crn`: a Prism group
wraps a styled layer so the order remains
`BevelEmboss -> OuterGlow -> MotionBlur`. An inline Aspect starts the glow Motion
while the sprite is visible. The game backend supplies only scene state and the
soft-drop blur value; it does not construct Prism images or effect graphs.

One connected silhouette atlas is shared by the active piece and its ghost.
Both are ordinary scene sprites with the same source geometry, while Prism is
scoped only around the active sprite draw. The realtime game surface renders in
`Continuous` mode; it does not use a synthetic Motion property as a frame clock.

The board retains a separate placement ID for every locked tetromino. Intact
locked pieces keep their joined silhouette instead of degrading into four
independent blocks, but are drawn directly from the atlas: landed pieces do not
retain animated Prism scopes. After a line clear, the surviving cells retain the
same placement ID and form their own connected fragment; neighboring pieces
never merge merely because they have the same kind or color. The retained
collection of intact landed sprites is rebuilt only when this locked board state
changes, not when the active piece moves or rotates.

Open `Tetris.slnx` to work with the game and its tests in Visual Studio.

## Run

Double-click the published Windows executable:

```text
Published\Cerneala.Tetris.exe
```

To run it from source, open PowerShell in this folder:

```powershell
dotnet run --project .\Tetris.csproj
```

## Controls

- Left / Right: move
- Down: soft drop
- Space: hard drop
- Up or X: rotate clockwise
- Z: rotate counter-clockwise
- C: hold
- P or Escape: pause
- R: restart

## Tests

```powershell
dotnet test .\Tests\Tetris.Tests.csproj
```
