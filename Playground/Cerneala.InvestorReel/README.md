# Cerneala Investor Reel

*Cerneala* is Romanian for ink. This is a self-playing film about the framework, about two
minutes long, told four times in four visual languages. The app opens on a picker; choose a
version and it plays from its first frame. At the end the last chapter asks whether to go back
to the picker or exit.

Every frame is built with Cerneala markup, Aspect, Motion and Prism. Every scene's `.crn.cs` is
an empty partial class. The only C# besides the verification harness is the shell's navigation
(`ReelWindow.crn.cs` and `ShellViewModel.cs`).

```powershell
dotnet run --project .\Playground\Cerneala.InvestorReel\Cerneala.InvestorReel.csproj
```

A cold Debug start can take a minute or more before the window appears.

Every screen is composed on a fixed 1600x900 DIP page. The page sits in a `Canvas`, which
measures it unconstrained, and is scaled by 0.8 so it fills a 1280x720 DIP window. At 125%
Windows scaling that window is 1600x900 physical pixels.

## The four versions

| Version | Look | Cuts between chapters |
| --- | --- | --- |
| Ink & Paper | The original. Paper, a manuscript margin, Sitka type, an editor's handwriting, a vermilion seal. | An ink blot floods the page from a different corner and ebbs away. One ink line runs along the foot of the page. |
| Cyberpunk | A rain-wet city at night with neon katakana signs, a HUD frame, Bahnschrift and Cascadia Mono, RGB-split headlines. The whole stage sits under a CRT layer (scanlines and chromatic aberration). | The CRT split spikes, glitch slices flash across the frame, and a twelve-segment progress bar fills. |
| Brutalist | Raw concrete with its twelve-column grid exposed, Impact and Courier New, hard offset shadows, one signal orange. Nothing eases gently. | A black slab slams across the frame carrying the next chapter number. The number is also cast huge behind each chapter. |
| Sakura | A spring garden: blush sky, soft hills, a blossoming branch and petals falling through every chapter. Gabriola and Palatino, kanji chapter numerals. | A gust of petals sweeps over a soft wash of paper. Twelve buds along a branch bloom, one per chapter. |

## Chapters

All four versions tell the same twelve chapters with the same facts.

| # | Chapter | What it shows |
| --- | --- | --- |
| 1 | Opening | The name *cerneala* (cher · nea · la, Romanian for ink) and the promise: a new way to write desktop software in C#. Retained, markup first, GPU rendered. |
| 2 | Describe it once | `.crn` is written line by line while a sketch becomes the real card. The source generator writes the C#. |
| 3 | One rule, every element | One Aspect restyles twelve tiles in a wave. When one token changes, all twelve follow. |
| 4 | Motion you can read | Tween, Spring, Keyframes and Stagger, each shown moving. |
| 5 | Effects | Shapes lifted by Motion-animated Prism `DropShadow` parameters, beside three animated Prism filter proofs (different filters per version). |
| 6 | Only what changes is redrawn | One figure in a ledger changes between frames; only it is redrawn, the rest is kept. |
| 7 | Bound once, updated from anywhere | A compiled `$DataContext` binding. A burst of worker-thread notifications collapses into one Relay refresh on the UI thread. |
| 8 | Worlds beside widgets | A Scene2D tile map, a sprite that hops a ledge and touches a pickup, box and circle colliders, importers and packages. |
| 9 | Tooling | A `.crn` editor catches a misspelt property, completion picks a brush, and the Live Preview changes in place without a rebuild. |
| 10 | Proof, not promises | A Servo script finds a button by `Servo.Id`, clicks it, waits for idle and saves a screenshot. Detective reads back the frame (sample counters). |
| 11 | One stack | C#, .crn, Aspect, Motion and Prism on SDL_GPU. One compiler, one runtime, one language. |
| 12 | Write interfaces | The closing line, then the question: choose another theme, or exit. |

## Files

| File | Role |
| --- | --- |
| `App.crn` | Every palette and type Aspect, one block per version (Ink, `Shell*`, `Cy*`, `Br*`, `Sk*`), plus each version's end-of-film button Aspect. |
| `ReelWindow.crn` / `.crn.cs` | The shell: the scaled page with the picker and a `ContentControl` for the current reel. The code-behind is navigation only: it creates a fresh reel per viewing, so every chapter starts from its declared first frame. |
| `ShellViewModel.cs` | `PlayCommand` (parameter: the theme name), `HomeCommand` and `ExitCommand`. The picker and every closing chapter bind to them. |
| `ThemePicker.crn` | The opening screen: four live posters, each a button bound to `PlayCommand`. |
| `Themes/<Version>/<X>Reel.crn` | Each version's director: its world, its furniture and one `MotionClip` (`Reel`) that plays the chapters in order. |
| `Themes/<Version>/*Scene.crn` | One `UserControl` per chapter, each with its own `Entrance` clip, started by `@when $self.Visibility`. |
| `Themes/Sakura/Blossom.crn` | A reusable cherry blossom, used by the picker and the Sakura version. |
| `ReelWindow.Capture.cs` | Verification harness only (see below). It plays no part in the film. |

## Authoring constraints found while building it

These shaped the markup. They are observations from this build, not fixes:

- **Custom `UserControl` types cannot be Motion targets by name.** The generator fails with
  CERNEALAUI021. Each chapter therefore sits inside a named `Grid` host, and its session
  reattaches when the host's effective visibility changes.
- **A clip can only target named elements inside the element that runs it.** The Cyberpunk REC
  light blinks from the director's clip, not from the city's ambience clip.
- **Don't start the reel from `@on Loaded` on a container.** `Loaded` fires during pre-order
  attach, before the clip's targets are attached.
- **A Motion session owned by the `Window` did not advance.** Each reel is owned by its stage
  `Grid` instead.
- **An animation on a collapsed element never finishes**, and stalls the sequence it is in.
  Waits before showing the end-of-film buttons run on a visible element.
- **`Thickness` has no literal form in Motion values.**
- **A `Grid` takes all the space it is offered**, in both star and auto rows, whatever its
  alignment. Grids that must not stretch have explicit sizes; centred content inside a
  stretched Grid needs its own alignment.
- **Text inside a transparent Prism layer did not render.** Filtered proofs keep an opaque
  backdrop inside the layer, and neon glows are drawn with gradient halos instead of glow
  styles on text.
- **An element with its own Prism layer covered overlapping siblings with an opaque
  rectangle.** Every lifted piece in the effects chapters is spaced so no two layers overlap.
- **A transparent overlay still takes hit testing.** Full-stage covers and wipes are marked
  `IsHitTestVisible="False"` so the end-of-film buttons stay clickable.
- **`@set` writes the Local value, which outranks Motion's Animation value.** After a property
  has been `@set`, later animations of it no longer show. Resets between animation cycles (the
  second burst in chapter 7) are one-frame `Tween(16ms)` animations instead; `@set` is kept for
  properties that are never animated, such as `Text` and `Visibility`.
- **Two animations on the same property of the same element cancel each other.** Inside a
  `@parallel`, that cancellation stops every sibling.
- **`@stagger` accepts only a plain `Tween`**, and `@from` is applied the moment an animation
  starts, even with a `Delay`.
- **A `forever` animation inside a `@parallel` keeps that block from ever finishing.** Anything
  that must follow it goes in a sibling `@sequence` inside the same block.
- **Text swaps are cross-fades between two elements**, rather than setting text that contains `$`.

## Capturing frames

Screenshots use the window-owned API, and input goes through Servo:

```powershell
$env:CERNEALA_REEL_THEME = "Sakura"                    # Ink, Cyberpunk, Brutalist, Sakura
$env:CERNEALA_REEL_CAPTURE_DIR = "$env:TEMP\reel"
$env:CERNEALA_REEL_CAPTURE_TIMES = "6500,17500,38900"  # ms after the poster click
$env:CERNEALA_REEL_THEN = "home"                       # optional: press an end button afterwards
$env:CERNEALA_REEL_CLOSE_AFTER_CAPTURE = "1"
dotnet run --project .\Playground\Cerneala.InvestorReel\Cerneala.InvestorReel.csproj
```

Without `CERNEALA_REEL_THEME`, times are measured from the first frame and show the picker.
Servo resolves targets in layout coordinates and ignores render transforms, so a harness run
shows the page at its native 1600x900 instead of the 0.8 fit-to-window scale.
