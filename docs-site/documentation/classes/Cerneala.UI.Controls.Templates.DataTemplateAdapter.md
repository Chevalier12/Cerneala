# DataTemplateAdapter Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/DataTemplateAdapter.cs`

Adapts a classic `DataTemplate` to the modern `ContentTemplate` pipeline.

```csharp
public sealed class DataTemplateAdapter : ContentTemplate
```

Inheritance:
`object` -> `ContentTemplate` -> `DataTemplateAdapter`

## Examples
Wrap a classic typed data template and create content through the modern content-template API:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;

DataTemplate legacyTemplate = new DataTemplate<string>(
    value => new TextBlock { Text = value });

ContentTemplate template = new DataTemplateAdapter(legacyTemplate);

UIElement? child = template.Create(new ContentTemplateContext("Saved"));
```

## Remarks
`DataTemplateAdapter` is a compatibility bridge for code that still has a classic `DataTemplate` but needs a `ContentTemplate`. The adapter derives from `ContentTemplate`, names itself with the `legacy.` prefix and the wrapped template data-type name, and exposes the classic template's `DataType` to the modern matching surface.

`Create(ContentTemplateContext)` ignores the base factory passed to `ContentTemplate` and delegates to the wrapped `DataTemplate.CreateElement(object?)` method using `context.Data`. The wrapped template performs its own data compatibility check and may return `null`; for example, `DataTemplate<T>` returns `null` for `null` data.

The adapter has no predicate, no key, and priority `0`. This means the inherited `CanApply(ContentTemplateMatchContext)` behavior matches by data type only when no requested key is present. The class exists as a legacy migration adapter; architecture tests assert that production code does not reference `DataTemplateAdapter` outside its own file after the modern template migration.

## Constructors
| Name | Description |
| --- | --- |
| `DataTemplateAdapter(DataTemplate template)` | Initializes an adapter for a classic data template. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the inherited diagnostic name, formatted as `legacy.` plus the wrapped template data-type name. |
| `DataType` | `Type?` | Gets the inherited accepted data type from the wrapped classic `DataTemplate`. |
| `Key` | `string?` | Gets the inherited template key. Always `null` for this adapter. |
| `Priority` | `int` | Gets the inherited match priority. Always `0` for this adapter. |
| `HasPredicate` | `bool` | Gets whether the inherited matching predicate is present. Always `false` for this adapter. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `CanApply(ContentTemplateMatchContext context)` | `bool` | Inherited from `ContentTemplate`; returns `true` when the context has no requested key and the data value matches the wrapped template's `DataType`. |
| `Create(ContentTemplateContext context)` | `UIElement?` | Creates content by passing `context.Data` to the wrapped classic `DataTemplate`. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `DataTemplateAdapter(DataTemplate template)` | `ArgumentNullException` | `template` is `null`. |
| `Create(ContentTemplateContext context)` | `InvalidOperationException` | The wrapped template rejects the supplied data value. |

## Applies To
Project: `Cerneala`

UI area: retained controls, legacy data templates, modern content-template migration.

## See Also
- `UI/Controls/Templates/DataTemplateAdapter.cs`
- `UI/Controls/Templates/ContentTemplate.cs`
- `UI/Controls/Templates/ContentTemplateContext.cs`
- `UI/Controls/DataTemplate.cs`
- `UI/Controls/DataTemplate{T}.cs`
