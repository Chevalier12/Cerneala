# AspectCatalog Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectCatalog.cs`

Represents a built aspect package catalog used by the aspect engine to resolve rules, templates, package diagnostics, and token default values.

```csharp
public sealed class AspectCatalog
```

Inheritance:
`object` -> `AspectCatalog`

## Examples

Build a catalog from registered packages and read a token default:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;

AspectToken<Color> accentToken = AspectToken.Color("app.accent");

AspectCatalog catalog = new AspectRegistry()
    .Register(AspectPackage.Create("App")
        .Tokens(tokens => tokens.Set(accentToken, Color.White)))
    .BuildCatalog();

if (catalog.TryGetTokenDefault(accentToken, out AspectValue value))
{
    // Resolve the AspectValue with an AspectResolutionContext when a concrete value is needed.
}
```

Use the catalog with `AspectEngine`:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCatalog catalog = new AspectRegistry()
    .Register(DefaultAspectPackage.Create())
    .BuildCatalog();

AspectEngine engine = new();
AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();
Button button = new();

engine.Apply(button, catalog, environment);
```

## Remarks

`AspectCatalog` is created by `AspectRegistry.BuildCatalog()`. It is a snapshot of the registry's registered packages at the registry version used to build it.

Catalog creation preserves package registration order. Package names are exposed through `PackageDiagnostics`, rules are appended into `Rules`, component template definitions are appended into `ComponentTemplates`, and content template definitions are appended into `ContentTemplates`.

Token defaults are stored by `AspectToken`. If later packages register the same token identity, the later default replaces the earlier value. If two packages register the same token name with different value types, catalog creation throws `InvalidOperationException`.

The catalog itself does not apply aspects. `AspectEngine` consumes `Rules` during resolution, while `AspectProcessor` synchronizes `TokenDefaults` into its environment when the catalog `Version` changes.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Version` | `int` | Gets the registry version captured when the catalog was built. |
| `PackageDiagnostics` | `IReadOnlyList<AspectPackageDiagnostic>` | Gets package diagnostic entries, currently one entry per registered package name in registration order. |
| `Rules` | `IReadOnlyList<AspectRuleSet>` | Gets all rule sets contributed by registered packages. |
| `ComponentTemplates` | `IReadOnlyList<ComponentTemplateDefinition>` | Gets component template definitions contributed by registered packages. |
| `ContentTemplates` | `IReadOnlyList<ContentTemplateDefinition>` | Gets content template definitions contributed by registered packages. |
| `TokenDefaults` | `IReadOnlyDictionary<AspectToken, AspectValue>` | Gets the registered default aspect values keyed by token. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `TryGetTokenDefault(AspectToken token, out AspectValue value)` | `bool` | Gets a token default from `TokenDefaults`. Throws `ArgumentNullException` when `token` is `null`; returns `false` when the token is not registered. |

## Applies to

Cerneala UI aspect package catalogs built from `AspectRegistry`.

## See also

- `AspectRegistry`
- `AspectPackage`
- `AspectEngine`
- `AspectProcessor`
