# Developer Preview Scope

This document defines the supported Cerneala Developer Preview surface. It is a guardrail for samples, docs, and tests, not a package split or a compatibility promise.

## Supported Surface

- Retained tree
- Typed UiProperty<T>
- Invalidation/frame scheduler
- Drawing command cache
- Input/routed events/focus/commands/input bindings
- Style/theme/default button template
- Core controls used by Authoring/Runtime samples
- Typed ObservableValue/ObservableList/BindingOperations
- TextBlock and single-line TextBox MVP
- ItemsControl/ListBox/ScrollViewer retained list path
- Resources/image cache/font resources
- MonoGame runtime adapter
- Platform services seams for cursor/clipboard/etc.
- Platform-neutral semantics tree
- Diagnostics and preview samples

## Deferred And Frozen Surface

- Package split
- Native accessibility adapters
- Full IME/multiline/rich text
- Markup/sourcegen expansion
- String-path binding as core hot path
- Advanced rendering/effects/path rendering/render targets
- Animation/storyboard expansion
- Advanced input categories beyond platform-backed seams

These areas may have prototype files or tests, but they are unsupported for Developer Preview samples unless a later plan explicitly promotes them.

## Naming Stance

Cerneala keeps WPF-like names where they improve developer ergonomics: `ControlTemplate`, `DataTemplate`, `ItemsPanelTemplate`, routed events, commands, and familiar control names. These names are not a compatibility promise. The Developer Preview is code-first, retained, strongly typed, and does not define XAML as the core authoring model.

String-path binding as core hot path is unsupported. Preview samples must use typed `BindingOperations`, observable values/lists, or explicit property adapters instead.

## Retained Game-Loop Contract

Update may run every frame.

Draw may run every frame.

Layout/render command generation must be invalidation-driven.

Unchanged frames should report no retained work.

Draw must not mutate retained work.
