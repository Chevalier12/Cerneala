# AspectRegistry Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectRegistry.cs`

Stores registered aspect packages, tracks registry changes, and builds `AspectCatalog` snapshots for aspect processing.

```csharp
public sealed class AspectRegistry
```

Inheritance:
`object` -> `AspectRegistry`

## Examples

Register packages and build a catalog:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;

AspectToken<Color> accent = AspectToken.Color("app.accent");

AspectCatalog catalog = new AspectRegistry()
    .Register(AspectPackage.Create("App")
        .Tokens(tokens => tokens.Set(accent, Color.White)))
    .BuildCatalog();
```

Use a change callback to react when the registry changes:

```csharp
using Cerneala.UI.Aspect;

bool changed = false;
AspectRegistry registry = new(() => changed = true);

registry.Register(AspectPackage.Create("App"));
```

## Remarks

Standalone registries capture their constructing thread. The registry exposed by `UIRoot` instead uses that root's Relay, so `Register` and `Unregister` throw `InvalidOperationException` before changing packages or `Version` when called from another thread.

`AspectRegistry` is the mutable package list that precedes `AspectCatalog`. `Register` appends an `AspectPackage`, rejects duplicate package names using ordinal string comparison, increments `Version`, and invokes the constructor callback when `notify` is `true`. `Unregister` removes a package by name, increments `Version`, and invokes the callback only when a package is actually removed.

`BuildCatalog()` creates an `AspectCatalog` from the current package order and current `Version`. The registry does not cache catalogs; callers that process aspects can compare catalog versions to decide whether token defaults need to be synchronized.

`UIRoot` creates an `AspectRegistry` with a callback that invalidates aspect processing for the subtree. Its default aspect package is registered with `notify: false` during root construction so initial setup does not trigger the registry-change invalidation path.

## Constructors

| Name | Description |
| --- | --- |
| `AspectRegistry(Action? changed = null)` | Initializes a registry and stores an optional callback invoked after notifying registration changes and successful unregister operations. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Version` | `int` | Gets the registry version. It starts at `0` and increments after each successful `Register` or `Unregister`. |
| `Packages` | `IReadOnlyList<AspectPackage>` | Gets the registered packages in registration order. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Register(AspectPackage package, bool notify = true)` | `AspectRegistry` | Adds `package`, increments `Version`, optionally invokes the change callback, and returns the same registry for chaining. |
| `Unregister(string packageName)` | `bool` | Removes the package with the specified name. Returns `true` when removed and `false` when no matching package exists. |
| `BuildCatalog()` | `AspectCatalog` | Builds a catalog snapshot from the registered packages and current registry version. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Register(AspectPackage package, bool notify = true)` | `ArgumentNullException` | `package` is `null`. |
| `Register(AspectPackage package, bool notify = true)` | `InvalidOperationException` | A package with the same `Name` is already registered. |
| `Unregister(string packageName)` | `ArgumentException` | `packageName` is `null`, empty, or whitespace. |

## Applies to

Cerneala UI aspect package registration, catalog building, root aspect invalidation, and aspect processor catalog synchronization.

## See also

- `Cerneala.UI.Aspect.AspectPackage`
- `Cerneala.UI.Aspect.AspectCatalog`
- `Cerneala.UI.Aspect.AspectProcessor`
- `Cerneala.UI.Elements.UIRoot`
