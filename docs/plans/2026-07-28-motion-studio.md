# Plan: Motion Studio

> Date: 2026-07-28
> Status: proposed
> Purpose: Add a separate Cerneala desktop application, called Motion Studio, that allows a user to visually build and run Motion components on a single shape, without user-written Motion code, Prism, or Aspect authoring.

## Vision

Motion Studio is a demo and exploration tool for the imperative Cerneala Motion API. The canvas contains exactly one global shape. The user creates named components, and each component describes an editable Motion tree that targets the same shape. Components are not visual layers and do not create other elements in the canvas.

The main stream is:

1. the user configures the global shape;
2. create or select a Motion component;
3. visually builds animations and compositions for the component;
4. press Play or Replay;
5. Motion Studio translates the editable definition into real Cerneala Motion calls;
6. the same global shape displays the result.

## Product decisions and architecture

- The canvas contains exactly one shape: `Rectangle`, `Ellipse` or `Path` SVG.
- The Photoshop inspired panel is called `COMPONENTS`, not `LAYERS`.
- A component is a named Motion program, not a visual element, control, or container.
- All components target the same shape instance in the canvas.
- Only one component runs at a time. Play on another component cancels the active execution with `KeepCurrent`, then starts the new component. This rule avoids invisible conflicts between two programs edited separately.
- `Play` starts from the current state of the shape. `Replay` cancels the execution, restores the baseline of the document and runs the selected component. `Reset Target` only restores the baseline. `Stop` keeps the current visual value.
- Inside a component, operations can run sequentially or in parallel. The Motion tree, not a pay list, is the source of truth.
- The editable model of the document remains separate from `MotionHandle`, `MotionGroupHandle` and the other runtime objects.
- The executor uses the Cerneala Motion API; the application does not implement a second interpolation engine or a parallel frame loop.
- The front-end exposes all Motion capabilities applicable to a single fixed element: compatible animated properties, From/To, Tween, Spring, Keyframes, Decay, Sequence, Parallel, start options, retarget mode, priority, hold-on-complete, cancel/complete and implicit transactions where they have useful semantics.
- Functionalities that require other targets or other context are not exposed: Stagger multi-target, layout transitions between elements, Presence, ScrollTimeline, Drag, Gestures or collection animations.
- `MotionStateBuilder` does not enter the UI until its public API provides executable behavior, not just an empty facade.
- Prism, `PrismInstance`, filters, styles, backdrops or Prism diagnostics are not used.
- Resources, rules, states or motion are not defined by Aspect. The application does not register its own Appearance package and does not use directives `@when`, `@animate`, `@parallel` or `@sequence`.
- The Cerneala Presentation style is reproduced locally through a C# palette and small factory/helpers for controls, without dependency on the `CernealaPresentation` project.
- The window and the UI tree are built in C#. The only markup allowed is the minimum `App.crn` required for the current desktop startup contract; it does not contain UI, Aspect or Motion.
- The first version does not save projects on disk, does not export video/code, does not implement undo/redo and does not allow plugins.

## Visual direction and layout

- Target window: 1360x880, usable up to minimum 1100x720.
- The local palette follows Presentation: ink `#0A0B0E`, panel `#14161B`, panel-alt `#101217`, raised `#191C22`, lines `#2A2E38`/`#424754`, paper `#EDEFF3`, slate `#8A93A6`, cyan `#4DF0FF`, pink `#FF3EA5`, lime `#C6FF3D`, orange `#FF8A3D`.
- `Cascadia Mono` is used for labels, values, status and diagnostics; the general text uses `Segoe UI Variable`.
- Main structure:
  - upper bar with title, playback status, Reset Target, Stop, Play and Replay;
  - left panel `COMPONENTS`, with creation, selection, renaming, duplication, deletion and Play;
  - closed central canvas, with discrete grid, visible bounds and unique shape;
  - contextual right inspector for the selected Motion shape or node;
  - the lower area `MOTION TREE`, with the component tree and add/reorder/delete actions.
- The layout takes the density and contrast of Presentation, but does not copy the tour, chapter navigation or Prism Studio content.

## Document model

- `MotionStudioDocument`
  - `TargetShapeDefinition Target`
  - `IList<MotionComponentDefinition> Components`
  - `Guid? SelectedComponentId`
- `TargetShapeDefinition`
  - the type of shape;
  - optional Path geometry;
  - width, height, fill, stroke and stroke thickness;
  - baseline for position, translate, scale and opacity;
  - render transform origin if the API of the shape allows it without speculative framework changes.
- `MotionComponentDefinition`
  - stable id;
  - valid and unique display name;
  - a single `MotionNodeDefinition Root`.
- Initial nodes:
  - `SequenceNodeDefinition`;
  - `ParallelNodeDefinition`;
  - `AnimationNodeDefinition`;
  - `SetValueNodeDefinition` only for explicit instant changes;
- `TransactionNodeDefinition` only if the feasibility stage confirms a clear correspondence with `BeginTransaction`.
- `AnimationNodeDefinition`
  - property selected from a standardized catalog;
  - From optional and To mandatory;
  - spec: Tween, Spring, Keyframes or Decay;
  - `MotionPropertyStartOptions`: retarget mode, priority, debug name and hold-on-complete.
- Keyframes have offset in `[0, 1]`, typed value, optional easing and hold; the first offset is 0, the last is 1, and the list remains sorted.

## Implementation stages

### Stage 0 - Capabilities contract and feasibility tests

- [ ] Inventory the Motion public API applicable to a single `UIElement` and write an internal array `capabilitate -> control front-end -> API Cerneala -> test`.
- [ ] Confirm through focused tests that an attached shape can correctly animate the basic properties `TranslateX`, `TranslateY`, `Scale` and `Opacity` through `MotionElementFacade`.
- [ ] Check the Shape properties that can be animated by `Animate<T>` and the registered mixers; does not display in the catalog properties without a mixer or without the correct rendering semantics.
- [ ] Checks sequencing, parallelism, cancellation, complete, retarget, priority and hold-on-complete on a single target.
- [ ] Checks if default transactions can be represented predictably by a visual node; remove `TransactionNodeDefinition` from scope if it does not bring a distinct capability to Parallel.
- [ ] Add only the Cerneala regression tests needed for real defects or gaps discovered by tests.
- [ ] If it is necessary to change a public API Cerneala, update the corresponding pages from `docs-site/documentation/classes/` in the same stage; do not extend the framework just for the convenience of the application.

**Gate Stage 0**

- [ ] The capabilities matrix is complete, each planned control has a verified runtime path, and the final scope does not promise incompatible Motion functionalities with a single target.

### Stage 1 - The standalone project and the programmatic shell

- [ ] Add `MotionStudio/MotionStudio.csproj` as application `net8.0-windows`/`WinExe`, with references to `Cerneala.csproj` and generator Cerneala.
- [ ] Add the project in `Cerneala.slnx` and configure the icon and the minimum required dependencies.
- [ ] Add `MotionStudio/App.crn` exclusively for `StartupWindow` and shutdown mode, without resources, Aspect or Motion markup.
- [ ] Add `MotionStudio/App.crn.cs` and `MotionStudio/MotionStudioWindow.cs`; fully builds the contents of the window in C#.
- [ ] Add `MotionStudio/Visual/MotionStudioPalette.cs` and DRY helper for labels, buttons, panels and dividers, without a styling mini-framework.
- [ ] Build the responsive layout with header, Components, canvas, inspector and Motion Tree.
- [ ] Add a test project `tests/MotionStudio.Tests/MotionStudio.Tests.csproj` and include it in the solution.

**Gate stage 1**

- [ ] Motion Studio starts as a separate application at the default and minimum size, without reference to `CernealaPresentation`, Prism or an application-defined Appearance package.

### Stage 2 - Editable model and front-end commands

- [ ] Implements the document models in `MotionStudio/Model/` as UI-independent objects and runtime handles.
- [ ] Implements the typed catalog of properties and specs in `MotionStudio/Motion/MotionCapabilityCatalog.cs`, fed explicitly from the stage 0 matrix.
- [ ] Implements validation for names, finite values, positive durations, Spring/Decay parameters, keyframes and tree structure.
- [ ] Implements the operations create/select/rename/duplicate/delete component and add/move/delete node as testable model services.
- [ ] Defines an initial deterministic document with an ellipse and three demonstrative components: `Entrance`, `Bounce` and `Exit`.
- [ ] Use `ActionCommand` for all UI actions and update `CanExecute` for selections, invalid root and active playback.
- [ ] Add tests for mutations, selection, deep-copy duplication, deletion of the last component, validation and the initial preset.

**Gate stage 2**

- [ ] The Motion document and tree can be completely edited in tests without building a window or starting the frame loop.

### Stage 3 - The compiler and the Motion playback session

- [ ] Implements `MotionComponentCompiler`, which recursively transforms definitions into real Motion calls and returns an aggregate handle owned by the session.
- [ ] Compile `Sequence` with lazy start of each step and `Parallel` with all child animations started in the same logical frame.
- [ ] Compiles Tween, Spring, Keyframes and Decay using public specs Cerneala and typed values ​​from the catalog.
- [ ] Apply From only when it is present, and To, retarget mode, priority, debug name and hold-on-complete exactly according to the definition.
- [ ] Implements `MotionPlaybackSession` with Idle, Playing, Completed, Canceled and Faulted states, without retaining terminal handles.
- [ ] Implements the exclusive policy: Play on another component cancels the active session with KeepCurrent.
- [ ] Implements Play, Replay, Stop and Reset Target according to plan decisions.
- [ ] Propagate compile/runtime errors in UI diagnostics without leaving active nodes or the shape in an invalid state.
- [ ] Add clock-controlled tests for order, parallelism, final values, deterministic replay, cancellation, component change and disposal.

**Gate stage 3**
- [ ] Presets and synthetic trees run exclusively through Cerneala Motion, have deterministic results and do not leave active handles/nodes after completion or cancellation.

### Stage 4 - Canvas and global shape inspector

- [ ] Builds the canvas with a centered viewport, clipping, discrete grid and bounds for the shape, without Prism or backdrop effects.
- [ ] Allows to change between Rectangle, Ellipse and Path keeping only one target attached.
- [ ] Connect the shape inspector to dimensions, fill, stroke, stroke thickness and baseline transform.
- [ ] When changing the shape type, cancel the playback, replace the target only once and reapply the baseline.
- [ ] Validates Path SVG and keeps the last valid geometry when the entered text is invalid.
- [ ] Displays the current runtime coordinates and values ​​separately from the baseline, so that Play does not rewrite the document.
- [ ] Add tests for target replacement, baseline/reset, invalid input, clipping and absence of additional visual copies in the canvas.

**Gate Stage 4**

- [ ] The canvas always has exactly one target shape, the changes in the inspector are immediate, and Reset Target completely restores the documented state.

### Stage 5 - Components and the Motion Tree editor

- [ ] Builds the Components panel with selection, create, rename, duplicate, delete, Play and status indicator per component.
- [ ] Build recursive Motion Tree for Sequence, Parallel, Animation, Set Value and Transaction only if it remained in scope.
- [ ] Allows adding a valid contextual child, moving up/down, moving between compatible containers and deleting without producing an invalid tree.
- [ ] Builds the contextual inspector for the composition and animation nodes.
- [ ] Generate typed editors for float, color/brush and any other type confirmed by the catalog, without string casting in the executor.
- [ ] Builds Tween, Spring, Keyframes and Decay editors; keyframes support add, delete, reorder by offset, easing and hold.
- [ ] Shows start options in an Advanced section and displays short explanations for retarget, priority and hold-on-complete.
- [ ] Visually marks the active node and the progress of the component without making the document model dependent on the runtime state.
- [ ] Add command and view-model tests for all editions and states `CanExecute`.

**Gate Stage 5**

- [ ] A user can build from the front-end a component with sequential and parallel animations, modify it and run it without writing code or markup.

### Stage 6 - Diagnostics, lifecycle and accessibility

- [ ] Displays the active component, session status, active node, elapsed time, active Motion nodes and the last cancellation/error reason.
- [ ] Add global stop, safe reset and cleanup when closing the window.
- [ ] Ensure visible focus, consistent Tab order, keyboard activation and accessible names for icon-only commands.
- [ ] Add documented shortcuts for Play/Replay, Stop, Reset Target, New Component and Delete Node using input commands Cerneala.
- [ ] Checks repeated component, shape and node changes during playback for subscription leaks and abandoned handles.
- [ ] Adds a deterministic automation/capture mode via environment variables, similar in intent to Presentation, but local to the Motion Studio project.

**Gate stage 6**

- [ ] The repeated playback and closing the window does not leave handles, callbacks or active subscriptions, and the main stream is fully usable from the keyboard.

### Stage 7 - Visual verification, performance and documentation

- [ ] Adds smoke tests for startup, initial preset, Play, Replay, Stop, component change and shape change.
- [ ] Capture and visually inspect the window at 1360x880 and 1100x720 for clipping, overlap, contrast, focus and readability.
- [ ] Check the frame budget during a component with Sequence + Parallel + Keyframes and confirm the lack of layout invalidation per frame for transform/opacity.
- [ ] Run the targeted Motion tests, `MotionStudio.Tests` and then `dotnet test .\Cerneala.slnx`.
- [ ] Run the reindexing and `doctor` through RoslynRepoIndexer after the final changes.
- [ ] Updates public documentation Cerneala only for public APIs changed in implementation; the application documentation remains a short README in `MotionStudio/README.md`.
- [ ] Run `git diff --check` and audit the project dependencies to confirm the absence of Prism, `CernealaPresentation` and Aspect authoring.

**Gate stage 7**

- [ ] All tests and verifications are green, the captures are correct at both sizes, the frame budget is acceptable and there is no temporary code or out-of-scope dependencies.

## Dependencies between stages

- Stage 0 blocks the catalog, the animation model and the executor.
- Stage 1 can start after establishing the startup contract and remains separate from the executor.
- Stage 2 depends on the matrix of stage 0.
- Stage 3 depends on the model of stage 2.
- Stage 4 depends on the shell of stage 1 and the baseline contract from stage 2.
- Stage 5 depends on stages 2-4.
- Stage 6 depends on the complete playback and UI.
- Stage 7 starts only after closing all the previous gates.

## Non-objectives

- several shapes simultaneously in the canvas;
- visual layers or z-order;
- import of Photoshop images or documents;
- Prism and any Prism filter/compositor;
- Aspect authoring, Aspect motion or copying Presentation resources;
- Motion markup;
- scroll-linked, gesture, drag, presence, layout motion or stagger multi-target;
- code editor, scripting console or C# generation in the first version;
- persistence, undo/redo, export video/GIF or project files;
- modification of Cerneala Motion semantics to accommodate an application workaround.

## The definition of ready

- [ ] Motion Studio is a standalone desktop project included in the solution and starts independently of Presentation.
- [ ] The canvas contains exactly one configurable global shape.
- [ ] The user can create several Motion components, all targeting the same shape.
- [ ] Each component can visually combine Sequence and Parallel animations and can use all Motion specs/options confirmed as applicable to a single target.
- [ ] Play, Replay, Stop, Reset Target and component change have deterministic and tested behavior.
- [ ] The user does not write code or markup to build the animations.
- [ ] The application does not depend on Prism, CernealaPresentation or Aspect authoring.
- [ ] The editable model is separated from the runtime handles and the executor uses the existing Cerneala Motion engine.
- [ ] Unit tests, integration, lifecycle, smoke, complete suite and visual inspection are green.
- [ ] Any public API Cerneala changed has the documentation from `docs-site/documentation/classes/` synchronized.