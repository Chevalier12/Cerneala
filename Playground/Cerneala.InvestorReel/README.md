# Cerneala Investor Reel

*Cerneala* is Romanian for ink. This self-playing film, roughly 80 seconds long, is built on
that idea: ink on paper, a manuscript margin, an editor's handwriting, a vermilion seal. It is
built entirely with Cerneala markup, Aspect, Motion and Prism. Every scene's `.crn.cs` is an
empty partial class.

```powershell
dotnet run --project .\Playground\Cerneala.InvestorReel\Cerneala.InvestorReel.csproj
```

A cold Debug start can take a minute or more before the window appears.

The film is composed on a fixed 1600x900 DIP page. The page sits in a `Canvas`, which measures
it unconstrained, and is scaled by 0.8 so it fills a 1280x720 DIP window. At 125% Windows
scaling that window is 1600x900 physical pixels.

## Chapters

| # | Chapter | What it shows |
| --- | --- | --- |
| i | A drop of ink | A drop falls and splashes, and the word *cerneala* rises out of the pool. |
| ii | A sketch becomes a screen | `.crn` is written line by line while a pen sketch turns into the real card, lifted by a Prism shadow. |
| iii | One rule, every element | One Aspect restyles twelve tiles in a wave. When the rule's token changes, all twelve follow. |
| iv | Movement, written as a score | Tween, Spring, Keyframes and Stagger as notes on a staff. Each note swells as the playhead reaches it. |
| v | Paper, lifted by light | Paper cut-outs rise on Motion-animated `DropShadow` parameters, beside SumiE, InkOutlines and Watercolor proofs. |
| vi | Change a word, not the page | One figure in a manuscript is corrected. Only it is redrawn; the rest is kept. |
| vii | One stack, one language | C#, .crn, Aspect, Motion and Prism fan out of one pile, resting on SDL_GPU. |
| viii | The seal | A vermilion seal is pressed onto the page beside the closing line. |

## Files

| File | Role |
| --- | --- |
| `App.crn` | The paper-and-ink palette, and the typography Aspects every chapter uses: Sitka for print, Ink Free for the editor's hand, Consolas for code. |
| `ReelWindow.crn` | The director. One `MotionClip` (`Reel`) plays the chapters in order. Between chapters an ink blot floods the page from a different corner, the chapter underneath is swapped, and the blot ebbs away. One ink line runs along the foot of the page for the whole film. |
| `*Scene.crn` | One `UserControl` per chapter, each with its own `Entrance` clip, started by `@when $self.Visibility`. |
| `ReelWindow.Capture.cs` | Verification harness only (see below). It plays no part in the film. |

## Authoring constraints found while building it

These shaped the markup. They are observations from this build, not fixes:

- **Custom `UserControl` types cannot be Motion targets by name.** The generator fails with
  CERNEALAUI021. Each chapter therefore sits inside a named `Grid` host, and its session
  reattaches when the host's effective visibility changes.
- **Don't start the reel from `@on Loaded` on a container.** `Loaded` fires during pre-order
  attach, before the clip's targets are attached.
- **A Motion session owned by the `Window` did not advance.** The reel is owned by the stage
  `Grid` instead.
- **`Thickness` has no literal form in Motion values.**
- **A `Grid` takes all the space it is offered**, in both star and auto rows. Grids that must
  not stretch have explicit sizes. This is also why the page is hosted in a `Canvas`.
- **Text inside a transparent Prism layer did not render.** The filtered proofs keep an
  opaque paper backdrop inside the layer.
- **Two animations on the same property of the same element cancel each other.** Inside a
  `@parallel`, that cancellation stops every sibling.
- **`@stagger` accepts only a plain `Tween`**, and `@from` is applied the moment an animation
  starts, even with a `Delay`. The playhead plucks in chapter iv are therefore individual
  sequences: wait until the playhead's known arrival time, then `PingPong(Tween(190ms), 2)`.
  The `2` means there and back.

## Capturing frames

Screenshots use the window-owned API:

```powershell
$env:CERNEALA_REEL_CAPTURE_DIR = "$env:TEMP\reel"
$env:CERNEALA_REEL_CAPTURE_TIMES = "6500,17500,38900"   # ms after first render
$env:CERNEALA_REEL_CLOSE_AFTER_CAPTURE = "1"
dotnet run --project .\Playground\Cerneala.InvestorReel\Cerneala.InvestorReel.csproj
```
