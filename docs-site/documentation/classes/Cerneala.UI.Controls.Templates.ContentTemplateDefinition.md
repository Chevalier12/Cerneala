# ContentTemplateDefinition Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/ContentTemplateDefinition.cs`

Describes a named content template contribution for an optional data type and key inside the aspect package catalog.

```csharp
public sealed class ContentTemplateDefinition
```

Inheritance:
`object` -> `ContentTemplateDefinition`

## Examples

Register a content template definition in an aspect package:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ContentTemplateDefinition definition = new(
    "Messages.Text",
    typeof(string),
    key: "message",
    template: new ContentTemplate<string>(
        "Messages.Text.Template",
        key: "message",
        priority: 0,
        context => new TextBlock { Text = context.Data ?? string.Empty }));

AspectPackage package = AspectPackage.Create("App")
    .Content(content => content.Add(definition));
```

Create a definition that records metadata without attaching the template payload yet:

```csharp
using Cerneala.UI.Controls.Templates;

ContentTemplateDefinition definition = new(
    "user-card",
    typeof(UserCard),
    key: "card",
    template: null);

public sealed record UserCard(string Name);
```

## Remarks

`ContentTemplateDefinition` is a small metadata container used by aspect packages. It records a non-empty `Name`, an optional `DataType`, an optional `Key`, and an optional `Template` payload.

`AspectPackageBuilder.Content` exposes `ContentTemplateBuilder.Add(ContentTemplateDefinition)`, which stores these definitions in the built `AspectPackage`. When an `AspectRegistry` builds an `AspectCatalog`, the catalog appends content template definitions from registered packages in package registration order.

The constructor validates only the definition name. It accepts a `null` data type, a `null` key, and a `null` template payload. The class does not resolve, instantiate, or match templates by itself; use `ContentTemplate`, `ContentTemplate<TData>`, and `ContentTemplateRegistry` for executable template matching and element creation.

## Constructors

| Name | Description |
| --- | --- |
| `ContentTemplateDefinition(string name, Type? dataType, string? key, object? template)` | Initializes a content template definition with a non-empty name, optional data type, optional key, and optional template payload. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the non-empty content template definition name. |
| `DataType` | `Type?` | Gets the data type associated with the content template contribution, or `null` when no data type is recorded. |
| `Key` | `string?` | Gets the optional content template key associated with the contribution. |
| `Template` | `object?` | Gets the optional template payload stored by the definition. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `ContentTemplateDefinition(string name, Type? dataType, string? key, object? template)` | `ArgumentException` | `name` is `null`, empty, or whitespace. |

## Applies to

Cerneala UI aspect packages and content template registration metadata.

## See also

- `Cerneala.UI.Aspect.AspectPackageBuilder`
- `Cerneala.UI.Aspect.ContentTemplateBuilder`
- `Cerneala.UI.Aspect.AspectCatalog`
- `Cerneala.UI.Controls.Templates.ContentTemplate`
- `Cerneala.UI.Controls.Templates.ContentTemplate<TData>`
- `Cerneala.UI.Controls.Templates.ContentTemplateRegistry`
