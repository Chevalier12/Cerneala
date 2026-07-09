# ContentTemplateBuilder Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectPackageBuilder.cs`

Adds content template definitions to an `AspectPackageBuilder` package composition.

```csharp
public sealed class ContentTemplateBuilder
```

Inheritance:
`object` -> `ContentTemplateBuilder`

## Examples

Add a content template definition while creating an aspect package:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls.Templates;

ContentTemplateDefinition stringTemplate = new(
    "App.StringContent",
    typeof(string),
    key: null,
    template: null);

AspectPackage package = AspectPackage.Create("App")
    .Content(content => content.Add(stringTemplate))
    .Build();
```

## Remarks

`ContentTemplateBuilder` is supplied to the callback passed to `AspectPackageBuilder.Content(Action<ContentTemplateBuilder>)`. Its constructor is internal, so package authors normally use it only inside that callback.

`Add(ContentTemplateDefinition)` appends the supplied definition to the package builder's pending content template list and returns the same `ContentTemplateBuilder` instance for fluent chaining. When the parent package builder is materialized with `Build()`, the collected definitions become the `AspectPackage.ContentTemplates` list.

The builder validates only that the added definition is not `null`. Template name validation is handled by `ContentTemplateDefinition`, and runtime matching or registration behavior is handled later by the content template infrastructure.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Add(ContentTemplateDefinition template)` | `ContentTemplateBuilder` | Adds a non-null content template definition to the pending package content templates and returns this builder. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Add(ContentTemplateDefinition)` | `ArgumentNullException` | `template` is `null`. |

## Applies to

Cerneala UI aspect package composition.

## See also

- `Cerneala.UI.Aspect.AspectPackageBuilder`
- `Cerneala.UI.Aspect.AspectPackage`
- `Cerneala.UI.Controls.Templates.ContentTemplateDefinition`
- `Cerneala.UI.Controls.Templates.ContentTemplate`
- `Cerneala.UI.Controls.Templates.ContentTemplateRegistry`
