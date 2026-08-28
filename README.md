# Cerneala

[![Desktop Backends](https://github.com/Chevalier12/Cerneala/actions/workflows/desktop-backends.yml/badge.svg)](https://github.com/Chevalier12/Cerneala/actions/workflows/desktop-backends.yml)

**A retained realtime UI framework for .NET applications and complete 2D games.**

Cerneala puts traditional application UI and realtime rendering inside the same retained runtime. You can build a calculator out of ordinary controls, or build a game where the game view itself is a control in the UI tree.

It is currently in **Developer Preview**. The architecture is real, the repository has a large test and verification surface, and a lot of the framework already works. The public experience is still being built, contracts can change, and some areas are much more mature than others.

![Cerneala retained realtime UI architecture](docs/assets/cerneala-architecture.png)

## Why I built it

I wanted something with the useful retained UI ideas from WPF and Avalonia, but designed for games and other realtime software from the beginning.

Putting a game inside a traditional desktop UI framework usually means working around a system that was built for a different job. Going in the other direction is not much nicer. Game engines often treat application UI as a secondary overlay system with its own rules, lifecycle, and limitations.

I did not want two worlds glued together.

I wanted one application model for windows, controls, layout, input, animation, rendering, HUDs, menus, tools, and the game itself. I also did not want the usual XAML ritual spread across markup, resources, converters, code-behind, and runtime magic.

Cerneala is my attempt to build that framework.

## The game is part of the UI

`RenderSurface2D` is a `ContentControl`. It participates in the same retained tree and lifecycle as the rest of the interface. Cerneala owns the frame integration, render target, presentation, and graphics state. Your code records the 2D drawing work for the frame.

Because it is a content control, normal Cerneala controls can live over the game surface. A HUD, menu, dialog, editor panel, or debug overlay does not need a separate UI integration layer.

```csharp
RenderSurface2D gameView = new()
{
    ClearColor = new Color(8, 11, 17),
    Content = new TextBlock
    {
        Text = "Score: 1200"
    }
};

gameView.Draw += (_, frame) =>
{
    frame.DrawSprite(player, playerBounds, Color.White);
};
```

That control can redraw continuously for a changing game scene, or on demand for static and infrequently changing content.

## What lives in Cerneala

Cerneala is bigger than a drawing surface. The repository contains the full path from authoring to native presentation.

### Retained UI core

The runtime owns typed state, logical and visual trees, properties, resources, layout, invalidation, retained render caches, input routing, focus, hit testing, commands, and control lifecycle.

The UI is created once. State changes invalidate the work that actually needs to be revisited. An unchanged frame should not rebuild the whole interface just because another frame happened.

### Realtime frame scheduling

Relay moves work onto the UI thread and refreshes bindings. The frame scheduler processes invalidation, layout, input, Motion, and rendering as explicit phases instead of hiding the work behind unrelated event loops.

### Aspect, Motion, and Prism

These are separate systems with separate jobs:

- **Aspect** owns tokens, rules, variants, states, and templates.
- **Motion** owns the root clock, animation graph, animated properties, and layout motion.
- **Prism** owns local visual composition, filters, styles, blending, masks, and backdrop effects without pretending that a visual effect changed layout or input.

### Typed authoring

Cerneala supports code-first UI and `.crn` markup. The markup is intentionally constrained. It goes through the Cerneala language tooling and source generator, which produce a typed C# UI tree.

Markup is allowed. Recreating the entire XAML language and all of its ceremony is not the goal.

The repository also contains a language server, Visual Studio integration, diagnostics, completion, navigation, formatting, and preview infrastructure for `.crn` files.

### Backend-independent drawing

UI and application code record drawing intent through `DrawingContext` and `DrawCommandList`. `IDrawingBackend` owns the backend boundary.

The repository currently contains rendering paths for:

- WindowsDX
- MonoGame and `SpriteBatch` as the existing transition path
- SDL3 GPU on native desktop platforms

The SDL3 GPU path uses **Cerberus**, Cerneala's GPU renderer for geometry and state batching, index rebasing, GPU uploads, and indexed draws.

SDL3 GPU is the strategic backend going forward. MonoGame will be discontinued
gradually. No removal version or date is committed yet, and some checked-in
projects still default to MonoGame today.

## AI-native, literally

"AI-native" does not mean that an AI generated some files and disappeared.

The repository ships with instructions, semantic navigation, scripts, harnesses, and task-specific skills intended for the agents that work on Cerneala. They are part of the development environment available to contributors, not private machinery that only exists on the author's computer.

The current repository skills cover work such as:

- researching and comparing algorithms before committing to one;
- reproducing and fixing bugs with a permanent RED regression test;
- creating evidence-backed implementation plans;
- executing plans one verified stage at a time;
- adversarially testing Cerneala subsystems;
- writing canonical API documentation.

The repository can also produce the evidence expected from a serious framework change: focused tests, full suites, native runtime smokes, screenshots through the application-owned capture API, pixel diffs, benchmarks, API diffs, and generated documentation checks.

An agent saying "this looks correct" is not evidence. A compiler accepting the code is not enough either. The repo is built around reproducible checks because the framework is too large for one person, or one model, to understand every graphics, mathematics, language, platform, and UI detail from intuition alone.

### [Maintainer's model policy](https://chevalier12.github.io/Cerneala/contributors.html#model-policy)

This is a contribution policy, not a general model leaderboard. For core engineering work:

- OpenAI GPT-5.6 or newer is approved and preferred;
- Anthropic Fable tier or a newer peer is approved;
- Anthropic Opus is specifically rejected, although it is allowed for visual work;
- older OpenAI models are rejected.

Anthropic models are welcome for CSS, composition, and visual direction. Unapproved Claude models are not a hidden shortcut for core engineering. The maintainer expects their fingerprints to show up in the diff.

The model is replaceable. The process and the contribution policy are not.

## Build the repository

The repository currently pins the .NET SDK version in [`global.json`](global.json). Install that SDK, then restore, build, and test from the repository root:

```powershell
dotnet tool restore
dotnet restore ./Cerneala.slnx
dotnet build ./Cerneala.slnx -c Release --no-restore
dotnet test ./Cerneala.slnx -c Release --no-build --no-restore
```

On Windows, run the playground with SDL3 GPU:

```powershell
dotnet run --project ./Playground/Cerneala.Playground/Cerneala.Playground.csproj -p:CernealaDesktopBackend=SDL3
```

The project currently defaults to MonoGame when the backend property is omitted:

```powershell
dotnet run --project ./Playground/Cerneala.Playground/Cerneala.Playground.csproj
```

The SDL3 GPU CI path also builds and runs native smoke scenarios on Windows, Linux, and macOS. The main playground currently targets Windows.

## Contributing

Issues and pull requests are welcome. They need to be documented, and technical claims need evidence.

For a bug report, include:

- the behavior you observed;
- the behavior you expected;
- the smallest reliable reproduction you have;
- platform, backend, inputs, and timing conditions when relevant;
- screenshots, traces, measurements, or logs when they matter.

For a pull request, include:

- the contract being changed or repaired;
- why the selected layer owns that contract;
- the regression test or other reproduction;
- the verification that ran;
- any required visual, performance, API, or documentation evidence;
- anything that remains unverified.

You do not need to manufacture all of this by hand. The point of the repository tooling and skills is to help produce it. Use them. If a harness disagrees with your explanation, the harness wins and the explanation needs to change.

Large changes should start with an issue or an evidence-backed checklist plan. Please do not bury architecture decisions, compatibility changes, or unrelated cleanup inside a convenient PR.

## Documentation

- [Cerneala website](https://chevalier12.github.io/Cerneala/)
- [API reference](https://chevalier12.github.io/Cerneala/documentation.html)
- [Roadmap](https://chevalier12.github.io/Cerneala/roadmap.html)
- [Benchmarks](https://chevalier12.github.io/Cerneala/benchmarks.html)
- [Getting started](docs/getting-started.md)
- [Cerneala markup guide](docs/CernealaMarkupGuide.md)
- [Architecture notes](architecture.md)

The canonical source for public API documentation is [`docs-site/documentation/classes/`](docs-site/documentation/classes/). Plans, audits, benchmark artifacts, tests, and generated reports are kept in the repository because they are part of the engineering record, not disposable chat history.

## Community

- [Discord](https://discord.gg/p6SbqByd59)
- [Contributors](https://chevalier12.github.io/Cerneala/contributors.html)

Questions, bug reports, implementation discussions, and documented pull
requests are welcome. Evidence still decides technical claims, even when the
conversation starts casually on Discord.

## License

Cerneala is available under the [MIT License](LICENSE).
