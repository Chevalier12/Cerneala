# Cerneala Project Memory

## Product Direction

Cerneala is a modern retained UI framework for C#/.NET, built on the current drawing and input foundations.

The target is "WPF 2026": familiar ideas where they still help, but without cloning legacy WPF compatibility, XAML-first design, Windows/HWND assumptions, or reflection-heavy magic.

## Existing Foundations

- `UI/Drawing` records backend-neutral drawing commands and renders them through adapters such as MonoGame.
- `UI/Input` provides input snapshots, routed event metadata, basic routing concepts, command primitives, and MonoGame input mapping.
- `architecture.md` documents these existing boundaries and must be read before changing drawing/input or building retained UI layers above them.
- `ROADMAPv2.md` is the active roadmap.

## Scope Bands

- MVP: retained UI tree, typed state, invalidation, layout, render cache, host integration, input/focus/commands, first controls, text services, resources, and playground validation.
- Core: styling, templates, scrolling, items, typed data observation, diagnostics.
- Later: text editing, accessibility, advanced rendering/media, animation, platform/package split.
- Optional/Experimental: markup, serialization, source generation, advanced input categories.

## Confirmed MVP Decisions

- Public base element name: `UIElement`.
- `Control` lives under `UI/Controls`; core element types live under `UI/Elements`.
- MVP color properties use existing `DrawColor`.
- Layout uses `float`.
- Layout geometry uses `LayoutSize`, `LayoutPoint`, and `LayoutRect`.
- MVP uses separate logical and visual trees.
- The new retained route model replaces `UiInputTree` as the future route table.
- Disabled, hidden, and collapsed elements are skipped by hit testing and do not receive pointer input.
- Invisible but layout-reserved elements cannot receive focus or input.
- MVP uses subtree render caches from the start.
- Input visual invalidation is driven by style metadata.
- Resource invalidation is driven by resource metadata.
- MVP processes dirty work deterministically; `FrameBudget` is later.
- Style/value precedence is `local > animation > style visual state > style base > inherited > default`.
- Templates are code-first until markup exists.
- Typed binding comes before string property paths.
- Template names `ControlTemplate`, `DataTemplate`, and `ItemsPanelTemplate` are kept.
- The repo stays one project through MVP.
- MonoGame remains the primary backend adapter after MVP.
- Platform services stay adapter-only/later until controls require them.
- Every new public type gets unit tests unless it is a trivial enum or marker.
- Retained no-work-frame tests are MVP blockers.
- Architecture boundary tests fail if UI core references MonoGame, Skia, HarfBuzz, or backend-specific types.
- Use both command-list assertions and visual golden-image tests after the first retained playground sample.

## Non-Goals

- Do not rebuild WPF.
- Do not make `DrawCommandList` a scene graph.
- Do not make `DrawingContext` own layout, styling, input, or control state.
- Do not add duplicate `Point`, `Rect`, `Color`, text, image, or input primitives unless the role is clearly higher-level.
- Do not make markup or serialization the core architecture.
- Do not split projects before MVP.
