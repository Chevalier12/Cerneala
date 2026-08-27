# AspectCatalog Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectCatalog.cs`

Represents a built aspect package catalog used to resolve rules, synchronize non-style behaviors, resolve templates, report package diagnostics, and provide token default values.

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

`AspectCatalog` is created by `AspectRegistry.BuildCatalog()`. It is an immutable snapshot of the registry's registered packages at the registry version used to build it. Rule, template, diagnostic, and token-default collections cannot be modified through casts to mutable collection interfaces.

Catalog creation preserves package registration order. Package names are exposed through `PackageDiagnostics`, catalog-owned rule projections are appended into `Rules`, component template definitions are appended into `ComponentTemplates`, and content template definitions are appended into `ContentTemplates`. A rule projection captures its package origin without mutating the reusable source rule, so separately built catalogs remain stable.

Token defaults are stored by `AspectToken`. If later packages register the same token identity, the later default replaces the earlier value. If two packages register the same token name with different value types, catalog creation throws `InvalidOperationException`.

The catalog itself does not apply aspects. `AspectEngine` consumes `Rules` during resolution, while `AspectProcessor` synchronizes `Behaviors` and projects `TokenDefaults` into its environment when the catalog `Version` changes.

`AspectProcessor` can compose a root snapshot with application, ancestor-scope, and element-local packages. Composed rules capture an internal source order used after layer order and before specificity; composed behavior, token, and template collections follow the same outer-to-inner visibility order. Composition creates another immutable catalog rather than mutating any source package or root snapshot.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Version` | `int` | Gets the registry version captured when the catalog was built. |
| `PackageDiagnostics` | `IReadOnlyList<AspectPackageDiagnostic>` | Gets the immutable package diagnostic snapshot in registration order. |
| `Rules` | `IReadOnlyList<AspectRuleSet>` | Gets immutable catalog-owned rule projections with stable package origins. |
| `Behaviors` | `IReadOnlyList<AspectBehavior>` | Gets the immutable ordered snapshot of non-style behaviors contributed by visible packages. |
| `ComponentTemplates` | `IReadOnlyList<ComponentTemplateDefinition>` | Gets the immutable component-template snapshot. |
| `ContentTemplates` | `IReadOnlyList<ContentTemplateDefinition>` | Gets the immutable content-template snapshot. |
| `TokenDefaults` | `IReadOnlyDictionary<AspectToken, AspectValue>` | Gets the immutable token-default snapshot. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `TryGetTokenDefault(AspectToken token, out AspectValue value)` | `bool` | Gets a token default from `TokenDefaults`. Throws `ArgumentNullException` when `token` is `null`; returns `false` when the token is not registered. |

## Applies to

Cerneala UI aspect package catalogs built from `AspectRegistry`.

## See also

- `AspectRegistry`
- `AspectPackage`
- `AspectBehavior`
- `AspectEngine`
- `AspectProcessor`
