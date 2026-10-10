# Theming And Resources

> Code: `UI/Theming`, `UI/Resources`, `UI/Aspect/ThemeTokenBridge.cs`, `UI/Elements/UIRoot.Subscriptions.cs`, `UI/Markup/GeneratedMarkupResources.cs` · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

**Theming** holds a small set of named values (colors, a palette, motion tokens) in an immutable `Theme`, and projects five of its colors into Aspect tokens. **Resources** looks up keyed values for an element: first in the element's own dictionary and its ancestors, then in the root's resource provider. Both systems turn a change into invalidation of the affected elements.

Neither system draws, measures or stores property values. A resource value reaches a property only through Aspect (an `AspectPackage` stored as a resource) or through a markup resource attachment (`GeneratedMarkup.AttachResource`). Image loading and image leases (`ImageResourceCache`, `ImageResourceLease`) use the same providers, but they are not described here.

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `Theme` | `UI/Theming/Theme.cs` | Value bag keyed by `(value type, key)`. `Set` returns the theme for chaining; after `Freeze` it throws | application code; frozen once installed, can be shared |
| `ThemeKey<T>` | `UI/Theming/ThemeKey{T}.cs` | Typed key (`Key`, `ValueType`) | value |
| `ThemeProvider` | `UI/Theming/ThemeProvider.cs` | Holds the current `Theme`; raises `ThemeChanged(OldTheme, NewTheme)` | one per `WindowApplicationRuntime`, shared by every window; or supplied by the host |
| `DefaultTheme` | `UI/Theming/DefaultTheme.cs` | Keys `PaletteKey`, `BackgroundKey`, `ForegroundKey`, `SurfaceKey`, `BorderKey`, `AccentKey`; `Create()` also sets `ThemeMotionTokens.Key` | static |
| `ThemePalette` | `UI/Theming/ThemePalette.cs` | Five colors: Background, Foreground, Surface, Border, Accent | value inside a theme |
| `ThemeResource<T>` | `UI/Theming/ThemeResource.cs` | Wraps a `ThemeKey<T>` and resolves it from a provider | no production use (text search) |
| `ThemeTokenBridge` | `UI/Aspect/ThemeTokenBridge.cs` | Projects theme colors into an `AspectEnvironment` | static; called by `AspectProcessor` |
| `ThemeChangedSubscription` | `UI/Elements/UIRoot.Subscriptions.cs` (private) | Connects one provider to one root; coalesces off-thread changes | one per `UIRoot` while a provider is set |
| `UiRelayRefreshDispatcher` | `UI/Relay/UiRelayRefreshDispatcher.cs` | Keeps at most one pending Relay post per activation | one per theme subscription |
| `IResourceProvider`, `IObservableResourceProvider` | `UI/Resources/IResourceProvider.cs`, `IObservableResourceProvider.cs` | Lookup contract; the observable one adds `ResourceChanged` | contract |
| `ResourceDictionary` | `UI/Resources/ResourceDictionary.cs` | Object-keyed observable provider; one `Version` counter for the whole dictionary | `Application.Resources` (one per application); `UIElement.Resources` (one per element) |
| `ResourceStore` | `UI/Resources/ResourceStore.cs` | Observable provider keyed by `(type, key)`, with a version per entry | no production instance (text search); used by tests and hosts |
| `ResourceId<T>`, `ResourceChangedEventArgs` | `UI/Resources/ResourceId{T}.cs`, `ResourceChangedEventArgs.cs` | Typed key; change event (`ResourceType`, `Key`, `OldValue`, `NewValue`, `Version`) | value |
| `ResourceDependencyTracker` | `UI/Resources/ResourceDependencyTracker.cs` | Maps a resource key to the elements that read it from the root provider | one per `UIRoot` (`UIRoot.ResourceDependencyTracker`) |
| `ResourceChangedSubscription` | `UI/Elements/UIRoot.Subscriptions.cs` (private) | Connects one observable provider to one root | one per `UIRoot` while an observable provider is set |
| `MarkupResourceController<T>` | `UI/Markup/GeneratedMarkupResources.cs` (private) | Writes a resource value into one property and refreshes it on change | one per markup resource reference; an element lifecycle behavior |

`WindowApplicationRuntime` wires every new window root the same way: `root.SetThemeProvider(themeProvider)`, then `root.SetResourceProvider(application?.Resources ?? resourceProvider)`. Without a supplied provider, the theme provider is `new ThemeProvider(DefaultTheme.Create())`.

## Data Flow

### Replacing the theme

A `Theme` cannot change after it is installed. The `ThemeProvider` constructor and the `Theme` setter both call the internal `Freeze()`. After that, `Set` throws `InvalidOperationException` with the message "This theme is installed in a ThemeProvider and cannot be changed. Build a new Theme and assign it to the provider." To change a color, build a new theme and assign it:

1. `provider.Theme = next`. The setter throws on `null`, returns at once when `next` is the same instance, freezes `next`, swaps the field and raises `ThemeChanged` on the calling thread. There is no thread check: the provider's `Theme` changes at once, on any thread.
2. Each root's `ThemeChangedSubscription` receives the event. On the root's UI thread it calls `InvalidateThemeChange()` at once. On another thread, `UiRelayRefreshDispatcher` posts one Relay callback and drops further requests while that callback is pending.
3. `InvalidateThemeChange()` calls `Invalidate(Aspect | Subtree, "Theme changed")` on the root.
4. In the next frame's Aspect phase, `AspectProcessor.GetEnvironment` sees that `root.ThemeProvider.Theme` is a different instance. It rebuilds the element's token environment in this order: framework token defaults, then `ThemeTokenBridge.Apply(theme, ...)`, then the catalog's explicit token defaults. A later step overwrites an earlier one. The existing `AspectEnvironment` instance is kept and filled with `ReplaceWith`.

Example (from `FirstPartyRelayIntegrationTests`):

```csharp
UIRoot root = new();
ThemeProvider provider = new(new Theme("initial"));
root.SetThemeProvider(provider);
root.ProcessFrame();

// On a worker thread:
for (int index = 0; index < 10_000; index++)
{
    provider.Theme = new Theme($"theme-{index}");
}

// Back on the UI thread:
// root.Relay.PendingCount == 1   (10,000 changes, one queued callback)
root.ProcessFrame();
// root.ThemeProvider.Theme.Name == "theme-9999"
```

`ThemeTokenBridge.Apply` reads only five keys: `Background`, `Foreground`, `Surface`, `Border` and `Accent`. For each one that is present it sets:

- the token `theme.<Key>` (from `ToToken`, for example `theme.Accent`);
- the matching `DefaultAspectTokens.Color.*` token;
- for Background, Foreground, Surface and Border, also the `DefaultAspectTokens.Brush.*` token, as a new `SolidColorBrush`;
- five Button tokens: `ButtonTokens.Background` from Surface, `Foreground` from Foreground, `BorderBrush` from Border, `HoverBackground` from Accent and `PressedBackground` from Border.

Other keys, for example `PaletteKey` or `ThemeMotionTokens.Key`, are not projected into tokens. Code reads them with `ThemeProvider.Get`.

Precedence example (from `ThemeTokenPrecedenceTests`), reading the `ButtonTokens.Background` token from a button's environment:

- With the default theme and `Surface` set to `(12, 34, 56)`, the token is a brush with color `(12, 34, 56)`.
- An `AspectPackage` that sets `ButtonTokens.Background`, stored under a resource key in an ancestor's `Resources`, overrides the theme. When several ancestors have one, the nearest one wins.
- Removing the packages one by one goes back to the outer package, then to the package in the root provider, then to the theme value.
- With `root.SetThemeProvider(null)`, the token falls back to the framework default, a `Color.White` brush. How Aspect collects packages from resources is in [aspect.md](aspect.md).

### Looking up a resource

`UIElement.TryFindResource<T>(key, out value)` searches in this order:

1. The element's own `Resources`, then each ancestor's. The walk goes to `LogicalParent` when there is one, otherwise to `VisualParent`.
2. If a dictionary on the way has the key but the value has the wrong type, the search stops and fails. A nearer wrong-typed entry hides the ancestors' entries.
3. When no dictionary has the key, and the key is a `string`, the root's `ResourceProvider` is asked with `new ResourceId<T>(key)`. A hit is recorded in `Root.ResourceDependencyTracker` with the default effect `Render`.

The untyped `TryFindResource(object, out object?)` does only step 1. It never asks the root provider.

Example (from `ElementResourceDictionaryTests`):

```csharp
ResourceStore host = new();
host.SetResource(new ResourceId<string>("HostValue"), "host");
UIRoot root = new();
root.SetResourceProvider(host);
StackPanel parent = new();
Border child = new();
root.LogicalChildren.Add(parent);
parent.LogicalChildren.Add(child);
parent.Resources["Accent"] = "parent";

child.FindResource<string>("Accent");                      // "parent"
child.FindResource(new ResourceId<string>("HostValue"));   // "host"
child.Resources["Accent"] = "child";
child.FindResource<string>("Accent");                      // "child"
```

A markup resource reference uses `GeneratedMarkup.AttachResource`. Its controller looks up the key with `owner.TryFindResource<T>`. When that fails it also tries `Application.Current?.Resources`. On a hit it writes the value with the markup value source, for example `UiPropertyValueSource.MarkupBase`.

### A root-provider resource changes

Example: `Application.Resources` holds `"Accent"`, and a `Border` in two windows has `Background` bound to it from markup.

1. `application.Resources.SetResource(new ResourceId<Brush>("Accent"), updated)`. `ResourceDictionary` stores the value and raises `ResourceChanged` with the dictionary-wide version. Setting a value equal to the stored one does nothing. `ResourceStore` behaves the same, with a version per entry.
2. Each root's `ResourceChangedSubscription` receives the event. It advances the root's image-resolution epoch. On the UI thread it calls `UIRoot.ApplyResourceChange` at once. On another thread it posts one Relay callback per event, so the events run in order (FIFO).
3. `ApplyResourceChange`:
   - invalidates `Aspect | Subtree` on the root when the old or new value is an `AspectPackage`;
   - asks `ResourceDependencyTracker.NotifyResourceChanged` for the elements that read this key. It drops detached elements first. For each remaining element it bumps the layout version (when the effects include Measure or Arrange) and the render version (when they include Render). Then it invalidates the element with `InvalidationFlags.Resource` and the recorded effects.
4. `DirtyPropagation` turns `Resource` into the recorded effects, or `Render` when none were given. The element goes into the matching queue.
5. Separately, each `MarkupResourceController` listening to the same provider checks the key. On the UI thread it re-reads and writes the property at once. On another thread it posts one Relay callback.

So both `Border` elements get the new brush. An element in the same window that does not read `"Accent"` stays clean. After one frame of render work, the next frame has no work.

### An element's own dictionary changes

`UIElement` subscribes to its own `Resources.ResourceChanged`. A change invalidates that element with `Resource | Aspect | Subtree` ("Element resources changed"). This dirties the whole subtree, because element-dictionary reads are not recorded in the dependency tracker. On another thread the invalidation is posted to the Relay. The callback is dropped if the element was detached and attached again in the meantime.

## Lifecycle And Ownership

- `UIRoot.SetThemeProvider` and `UIRoot.SetResourceProvider` call `Relay.VerifyAccess()`, so they run only on the root's UI thread. Setting the same instance again does nothing.
- Replacing a provider disposes the old subscription first. A disposed theme subscription deactivates its dispatcher, so a callback that is already queued finds a newer generation and does nothing. A disposed resource subscription ignores its queued callbacks. Then the root invalidates `Aspect | Subtree` ("Theme provider changed") or `Resource | Aspect | Subtree` ("Root resource provider changed").
- Both subscriptions hold the root through a `WeakReference`. If the root was collected, the next event disposes the subscription.
- `ResourceDependencyTracker` entries are removed lazily: an element that is no longer attached is dropped the next time its key changes, or by `RemoveOwner`.
- A `MarkupResourceController` subscribes to `owner.Root.ResourceProvider` in `Attach` and unsubscribes in `Detach`. `Dispose` also removes it from the element's lifecycle behaviors. After `Dispose`, a callback that was already queued does nothing.
- A frozen `Theme` never becomes writable again, also after it is replaced.

## Frame Integration

- Theme changes go through the Aspect phase: `Invalidate(Aspect | Subtree)` fills the Aspect queue, and `AspectProcessor` rebuilds each element's environment when the theme instance or the catalog version changed. Otherwise it reuses the cached environment.
- Resource changes go through the queue picked by the recorded effects, which is the Render queue for the default effect.
- Off-thread notifications become Relay callbacks. `UIRoot.BeginUpdate` runs them before the scheduler's phases, so the work they invalidate is processed in the same frame. See [relay.md](relay.md) and [invalidation-and-frame.md](invalidation-and-frame.md).

## Invariants

- An installed theme rejects `Set` with the message above, and its existing values still read the same. `tests/Cerneala.Tests/UI/Theming/ThemeTests.cs:ProviderInstallationRejectsThemeChangesAndPreservesReads`.
- Replacing the theme freezes the new one before `ThemeChanged` runs, raises exactly one notification, and leaves the old one frozen. `ThemeTests.cs:ReplacementFreezesNewThemeBeforeNotificationAndNeverUnfreezesOldTheme`.
- One frozen theme can be installed in two providers, and assigning the same instance raises no notification. `ThemeTests.cs:FrozenThemeCanBeSharedAndSameInstanceAssignmentDoesNotNotify`.
- 10,000 theme replacements on a worker thread leave one Relay callback, and the last theme wins. A change from a replaced provider is ignored. `tests/Cerneala.Tests/UI/Relay/FirstPartyRelayIntegrationTests.cs:ThemeChangesCoalesceAndProviderReplacementInvalidatesQueuedWork`.
- Lookup uses the nearest ancestor dictionary first, then the root provider. A nearer entry shadows a farther one. `tests/Cerneala.Tests/UI/Resources/ElementResourceDictionaryTests.cs:LookupUsesNearestLogicalAncestorThenRootProvider`.
- A wrong-typed entry hides the ancestors' entries. `ElementResourceDictionaryTests.cs:ExistingWrongTypedResourceShadowsAncestors`.
- Changing an element's dictionary marks it dirty with `Render` and `Subtree`. `ElementResourceDictionaryTests.cs:MutatingElementResourcesInvalidatesItsSubtree`.
- A change to an application resource updates the consumers in two roots, leaves an unrelated element clean, and the frame after the render work is idle. `tests/Cerneala.Tests/UI/Resources/ApplicationResourceIntegrationTests.cs:GlobalResourceUpdatesAllRealConsumersAndReturnsToIdle`.
- A local element entry shadows the application value, and the shadowed element is not recorded as a dependent. A root created later sees the latest application value. `ApplicationResourceIntegrationTests.cs:LocalResourceShadowsApplicationAndLaterRootsSeeLatestValue`.
- A worker-thread change to a bound resource is applied on the next frame (two Relay callbacks: the root and the attachment). `ApplicationResourceIntegrationTests.cs:ActiveResourceAttachmentAppliesWorkerNotificationOnTheNextFrame`.
- A disposed markup resource attachment ignores queued and later changes and keeps its last value. `ApplicationResourceIntegrationTests.cs:DisposedResourceAttachmentIgnoresPendingAndFutureProviderChanges`.
- Worker-thread resource events run in FIFO order, and events from a replaced provider are ignored. `FirstPartyRelayIntegrationTests.cs:ResourceChangesKeepFifoAndIgnoreReplacedProviders`.
- Each worker-thread change to an element's dictionary becomes one Relay callback, and a callback queued before a detach and re-attach is dropped. `FirstPartyRelayIntegrationTests.cs:ElementResourceChangesMarshalEachDeltaThroughRelay`.
- Nearer Aspect packages win; removing them restores the root provider's package, then the theme value; without a theme the framework default applies. The element's environment instance is reused, and the root is idle afterwards. `tests/Cerneala.Tests/UI/Aspect/ThemeTokenPrecedenceTests.cs:NearerScopesWinAndRemovalRestoresApplicationThenCurrentTheme`.
- Replacing the root resource provider while a markup resource attachment is attached: netestat (see Known Limitations).

## Diagnostic

Theming and resources have no Detective counters of their own. Their invalidations appear in `root.Detective.Invalidation` with these reasons: "Theme provider changed", "Theme changed", "Root resource provider changed", "Application Aspect package changed", "Resource changed" and "Element resources changed". See [detective.md](detective.md).

## Known Limitations

- Every theme change invalidates Aspect for the whole tree. There is no per-key comparison between the old and the new theme. Cost: nemăsurat.
- `ThemeTokenBridge` projects only the five color keys. A custom key reaches Aspect only if code or an Aspect package reads it.
- `ThemeProvider` has no thread check. The provider's `Theme` changes on the calling thread at once, while the roots react on their own UI thread.
- `ResourceDictionary` and `ResourceStore` use a plain `Dictionary` with no lock. Only the notifications are moved to the UI thread; the stored value is written on the calling thread. Concurrent reads and writes: netestat.
- `ResourceDictionary.Version` counts changes for the whole dictionary, not per key. `ResourceDependencyTracker.GetResourceVersion` stores that number for the changed key.
- The root provider is asked only by `TryFindResource<T>` and only for `string` keys.
- Only root-provider reads are tracked as dependencies. A change in an element's dictionary invalidates that element's whole subtree.
- `ResourceDependencyTracker.Track` has no production caller (text search). Roots feed the tracker through `ApplyResourceChange` instead.
- A `MarkupResourceController` subscribes to the root provider that is set when the element attaches. The source does not subscribe it again when `UIRoot.SetResourceProvider` replaces the provider while the element stays attached. This is inferred from the source; no test covers it.
- `ThemeResource<T>` and `ResourceStore` have no production use (text search).
