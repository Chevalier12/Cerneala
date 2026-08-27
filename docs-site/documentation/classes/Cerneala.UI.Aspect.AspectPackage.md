# AspectPackage Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectPackage.cs`

Represents a named bundle of aspect token defaults, rule sets, non-style behaviors, component templates, and content templates that can be registered with an `AspectRegistry`.

```csharp
public sealed class AspectPackage
```

Inheritance:
`object` -> `AspectPackage`

## Examples

Create and register a package that contributes a token default and a component template:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

AspectToken<Color> accent = AspectToken.Color("app.accent");
ComponentTemplateDefinition buttonTemplate = new("App.Button", typeof(Button), ButtonTemplates.Modern);

AspectPackage package = AspectPackage.Create("App")
    .Tokens(tokens => tokens.Set(accent, new Color(37, 99, 235)))
    .Components(components => components.AddTemplate(buttonTemplate));

AspectCatalog catalog = new AspectRegistry()
    .Register(package)
    .BuildCatalog();
```

Create a package that contributes a content template definition:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls.Templates;

ContentTemplateDefinition definition = new("user-card", typeof(UserCard), key: "card", template: null);

AspectPackage package = AspectPackage.Create("Content")
    .Content(content => content.Add(definition));

private sealed record UserCard(string Name);
```

## Remarks

`AspectPackage` is the transport object used by the aspect system before packages are merged into an `AspectCatalog`. Create packages with `Create(string)`, configure them through `AspectPackageBuilder`, and pass the result to `AspectRegistry.Register`.

The public constructor surface is intentionally closed; the class is created by the builder. Package names must be non-empty and non-whitespace. The internal constructor also rejects `null` token, rule, component template, and content template lists.

The builder inputs are copied into immutable package snapshots. Callers cannot mutate token, rule, behavior, component-template, or content-template collections by retaining or casting the lists used to construct the package.

When `AspectRegistry.BuildCatalog()` combines packages, it preserves package registration order, appends catalog-owned rule projections and templates, and records each package name in diagnostics without mutating source rules. Duplicate package names are rejected by `AspectRegistry.Register`. Token defaults with the same token identity can be replaced by later packages, but catalog creation throws `InvalidOperationException` when the same token name is registered with different value types.

`AspectPackage` does not apply aspect values by itself. `AspectEngine`, `AspectProcessor`, template registries, and related services consume the catalog built from registered packages. `AspectProcessor` also synchronizes package `Behaviors`; those sidecars remain outside value matching and cascade.

`Origin` is immutable diagnostics metadata. It records code/markup authoring origin but does not participate in cascade comparison.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the package name used for registration, diagnostics, and duplicate-package checks. |
| `Origin` | `AspectOrigin` | Gets immutable authoring-origin metadata copied into catalog rule projections. |
| `Tokens` | `IReadOnlyList<AspectTokenDefinition>` | Gets the immutable token-definition snapshot contributed by the package. |
| `Rules` | `IReadOnlyList<AspectRuleSet>` | Gets the immutable rule snapshot contributed by the package. |
| `Behaviors` | `IReadOnlyList<AspectBehavior>` | Gets the immutable non-style behavior snapshot contributed by the package. |
| `ComponentTemplates` | `IReadOnlyList<ComponentTemplateDefinition>` | Gets the immutable component-template snapshot contributed by the package. |
| `ContentTemplates` | `IReadOnlyList<ContentTemplateDefinition>` | Gets the immutable content-template snapshot contributed by the package. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Create(string name)` | `AspectPackageBuilder` | Creates a builder for a package named `name`. The builder validates that the name is not empty or whitespace. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Create(string name)` | `ArgumentException` | `name` is `null`, empty, or whitespace. |

## Applies to

Cerneala UI aspect package registration, catalog building, aspect resolution, and component/content template contribution.

## See also

- `Cerneala.UI.Aspect.AspectPackageBuilder`
- `Cerneala.UI.Aspect.AspectRegistry`
- `Cerneala.UI.Aspect.AspectCatalog`
- `Cerneala.UI.Aspect.AspectTokenDefinition`
- `Cerneala.UI.Aspect.AspectRuleSet`
- `Cerneala.UI.Aspect.AspectBehavior`
- `Cerneala.UI.Controls.Templates.ComponentTemplateDefinition`
- `Cerneala.UI.Controls.Templates.ContentTemplateDefinition`
