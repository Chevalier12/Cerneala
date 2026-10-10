# Accessibility

> Code: `UI/Accessibility`, `UI/Platform/IAccessibilityPlatform.cs`, `UI/Elements/UIRoot.cs` (semantics cache) · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

Accessibility builds a **semantics tree**: an immutable snapshot of the visual tree in which every element has a role (Button, Text, List, ...), a name and a few properties. The tree is built on demand, from the current element state.

**The tree reaches no platform today.** `IAccessibilityPlatform` is a contract with no production implementation, and no production code calls it. `Cerneala.Platforms.Sdl3` passes no accessibility service, and the repository has no UI Automation, AT-SPI or NSAccessibility adapter. A screen reader therefore sees nothing from a Cerneala window. The current consumers are tests and Servo's in-process queries (see [servo.md](servo.md)).

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `SemanticsProvider` | `UI/Accessibility/SemanticsProvider.cs` | Stateless builder. Public `Build(UIRoot)` uses the Accessibility projection; internal `Build(root, projection)` also serves Servo | one per `UIRoot`; one per Servo query engine |
| `SemanticsProjection` | same file (internal enum) | `Accessibility` (skips non-rendered subtrees) or `Servo` (keeps them) | value |
| `SemanticsTree` | `UI/Accessibility/SemanticsTree.cs` | Immutable wrapper with `Root` | snapshot; the last one is cached by `UIRoot` |
| `SemanticsNode` | `UI/Accessibility/SemanticsNode.cs` | Immutable node: `ElementId`, `Role`, `Name`, `Properties`, `Children`, `GetProperty<T>` | snapshot |
| `SemanticsRole`, `SemanticsProperty` | `UI/Accessibility/SemanticsRole.cs`, `SemanticsProperty.cs` | Role and property-key enums | contract |
| `AutomationPeer` | `UI/Accessibility/AutomationPeer.cs` | Per-element adapter that produces one node; `Create` picks the peer type with a type switch | transient, created during one build |
| `ButtonAutomationPeer`, `TextBoxAutomationPeer`, `PasswordBoxAutomationPeer`, `ItemsControlAutomationPeer`, `MenuAutomationPeer`, `MenuItemAutomationPeer` | `UI/Accessibility/*AutomationPeer.cs` | Control-specific role, name and properties | transient |
| `AccessibleName` | `UI/Accessibility/AccessibleName.cs` | Attached `NameProperty` (`AffectsSemantics`; blank becomes `null`) and the content-text fallback | element property value |
| `IAccessibilityPlatform` | `UI/Platform/IAccessibilityPlatform.cs` | `void Publish(SemanticsTree tree)` | no production implementer |

`PlatformServices.Accessibility` is the slot for an `IAccessibilityPlatform`. It defaults to `null`, and the SDL3 platform does not set it.

## Data Flow

### Building a tree

`SemanticsProvider.BuildNode` walks `VisualChildren` depth first, in visual order:

1. For each child, the Accessibility projection skips it, with its whole subtree, when `UIElementVisibility.ParticipatesInRendering(child)` is false (`IsVisible` is false, or `Visibility` is not `Visible`).
2. The Servo projection keeps every child, and also adds the children of an `IInputSubtreeHost` (for example `SceneNode2D` and `RenderSurface2D`).
3. `AutomationPeer.Create(element)` picks the peer, and `CreateNode(children)` builds the node.

Peer selection, in order: `MenuItem`, `MenuBar`, `Menu`, `Button`, `TextBox`, `PasswordBox`, `ItemsControl`, `TextBlock` (role `Text`), and everything else. The default peer has role `Root` for a `UIRoot` and `Group` otherwise. Its properties are `IsEnabled` and `IsFocused` (`IsKeyboardFocused`).

The name comes from `AccessibleName.NameProperty` when it is set. Otherwise it is the content text: a non-blank `string`, a `TextBlock.Text`, or the content of a `Button`, `ContentControl` or `ContentPresenter`.

Example:

```csharp
UIRoot root = new();
root.VisualChildren.Add(new Button { Content = "Save", IsEnabled = false });
SemanticsNode node = new SemanticsProvider().Build(root).Root.Children.Single();
// node.Role == SemanticsRole.Button
// node.Name == "Save"
// node.GetProperty<bool>(SemanticsProperty.IsEnabled) == false
```

With `AccessibleName.SetName(button, "Explicit")`, the name becomes `"Explicit"`. A `PasswordBox` has role `EditableText` and a `null` value, so the password is never exposed.

The walk includes template parts, because nodes have no "presentational" flag. Once the template is applied, a templated `Button` gets child nodes for its template elements. The source shows a `Border` (role `Group`), a `ContentPresenter` (role `Group`, name `"Save"`) and a generated `TextBlock` (role `Text`, name `"Save"`). This child shape is inferred from the source; no test asserts it.

### The `UIRoot` cache

`UIRoot.GetSemanticsTree()` returns the cached tree while `semanticsDirty` is false and `TreeVersion` has not changed. Otherwise it rebuilds the whole tree. There is no per-node reuse.

`semanticsDirty` becomes true:

- when the root starts;
- when an `Invalidate` request carries `InvalidationFlags.Semantics`. A property registered with `UiPropertyOptions.AffectsSemantics` produces that flag when its value changes. Examples: `UIElement.IsEnabled`, `IsVisible`, `Visibility`, `IsKeyboardFocused`, `AccessibleName.NameProperty`, `ContentControl.Content`, `TextBlock.Text`, `TextBox.Text`, `MenuItem.Header`.
- in `UIRoot.IncrementTreeVersion`, which runs when a child collection changes.

Servo does not use this cache. It calls `Build(root, SemanticsProjection.Servo)` for every query.

## Lifecycle And Ownership

- Each `UIRoot` creates one `SemanticsProvider` and holds the last built `SemanticsTree`, the `TreeVersion` it was built at, and the `semanticsDirty` bit. A new root starts dirty.
- A tree is an immutable snapshot. A later change never edits it; the next `GetSemanticsTree()` call builds a new tree and replaces the cached one. The old tree stays valid for whoever still holds it.
- Automation peers are created during one build and are not kept. Nothing in the system needs to be disposed.
- A node refers to its element only through `ElementId`. `root.ElementIds` resolves that id only while the element is attached to that root. After a detach, the id in an old snapshot no longer resolves.
- Servo builds its own tree for each query and does not keep it in the root.
- `AccessibleName.NameProperty` is an ordinary property value on the element and lives as long as the element.

## Frame Integration

Semantics adds no frame phase and no queue. `InvalidationFlags.Semantics` only sets `semanticsDirty` and the element's dirty bit. It is not one of the flags that make `UiFrameScheduler.HasWork` true. Nothing is rebuilt or pushed during a frame. The rebuild happens only when someone calls `GetSemanticsTree()`. See [invalidation-and-frame.md](invalidation-and-frame.md).

## Invariants

- An unchanged root returns the same cached tree instance, also over 50 repeated queries on a large tree. `tests/Cerneala.Tests/UI/Accessibility/RetainedSemanticsCacheTests.cs:SemanticsProviderCachesUnchangedRootSemantics`, `tests/Cerneala.Tests/UI/Accessibility/SemanticsStressBudgetTests.cs:SemanticsRepeatedQueriesReturnCachedTreeForLargeTree`.
- An accessible-name change rebuilds the tree without measure, arrange or render work, and without changing `TreeVersion`. `RetainedSemanticsCacheTests.cs:AccessibleNameChangeInvalidatesSemanticsWithoutLayoutOrRender`, `SemanticsStressBudgetTests.cs:SemanticNameChangeRebuildsSemanticsWithoutLayoutOrRenderBudget`.
- Adding a child invalidates the cached tree. `RetainedSemanticsCacheTests.cs:TreeMutationInvalidatesSemanticsCache`.
- Adding an item to an `ObservableList` bound to a `ListBox` changes `ItemCount` from 1 to 2. `tests/Cerneala.Tests/UI/Accessibility/AuthoringSemanticsContractTests.cs:ObservableListMutationInvalidatesListSemantics`.
- The Accessibility projection omits hidden and collapsed subtrees; the Servo projection keeps them in visual order. `tests/Cerneala.Tests/UI/Accessibility/SemanticsProviderTests.cs:DefaultProjectionOmitsNonRenderedSubtrees`, `SemanticsProviderTests.cs:ServoProjectionIncludesNonRenderedSubtreesInStableOrder`.
- An explicit accessible name overrides content text. `SemanticsProviderTests.cs:ExplicitAccessibleNameOverridesContentText`.
- A node's `ElementId` resolves through `root.ElementIds` only while the element is attached. `SemanticsProviderTests.cs:ElementIdResolvesOnlyWhileElementBelongsToRoot`.
- A button reports role, name and enabled state. `tests/Cerneala.Tests/UI/Accessibility/ButtonSemanticsTests.cs:ButtonExposesRoleNameAndEnabledState`.
- A password box never exposes its value. `tests/Cerneala.Tests/UI/Accessibility/TextBoxSemanticsTests.cs:PasswordBoxDoesNotExposePasswordValue`.
- `ButtonAutomationPeer.Invoke` clicks an enabled button and does nothing on a disabled one. `ButtonSemanticsTests.cs:ButtonAutomationPeerInvokesEnabledButtonThroughClickRoute`, `ButtonSemanticsTests.cs:ButtonAutomationPeerDoesNotInvokeDisabledButton`.
- `IAccessibilityPlatform.Publish` receives the snapshot unchanged. This is tested only against a test-local recorder: `tests/Cerneala.Tests/UI/Accessibility/AccessibilityPlatformTests.cs:PlatformBoundaryReceivesSemanticsTreeSnapshot`.
- Changing `Visibility` after the tree was built removes the subtree from the next tree: netestat.

## Diagnostic

Detective reports only whether an accessibility service is present (`Accessibility is not null`). See [detective.md](detective.md).

## Known Limitations

- No platform adapter (see Responsibility). The archived roadmap says "contract exists; real adapter behavior remains later" ([roadmap-v2.md](../archive/roadmap-v2/roadmap-v2.md)).
- `GetSemanticsTree` has no production caller, and it does not call `Relay.VerifyAccess`.
- Roles `None`, `ListItem` and `Image`, and the property `IsSelected`, are declared but never produced.
- `CheckBox`, `RadioButton`, `ToggleButton` and `RepeatButton` all become role `Button`, with no checked state. `ComboBox`, `ListBox` and `TabControl` become role `List`. Controls without a dedicated peer become role `Group`.
- Peer selection is a fixed type switch, with no per-element override.
- Some `AffectsSemantics` properties are not read by any peer, for example `IsPointerOver`. Changing them still marks the tree dirty, so the next query rebuilds an identical tree.
- `ButtonAutomationPeer.Invoke` has no production caller. Servo actions use synthesized input, not peers.
