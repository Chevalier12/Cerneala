# WPF/Microsoft Learn-Style API Documentation Formula

Use this reference when creating Markdown documentation for WPF-like classes, controls, properties, methods, and events.

## Class Page

````md
# ClassName Class

## Definition
Namespace: `...`
Assembly/Project: `...`
Source: `path/to/ClassName.cs`

One-sentence summary.

```csharp
public class ClassName : BaseClass, IInterface
```

Inheritance:
`Object` -> `BaseClass` -> `ClassName`

Derived:
...

Attributes:
...

Implements:
...

## Examples
Minimal example using real APIs.

## Remarks
What the class is for.
Important behavior.
Framework-specific quirks.
Styling/template/layout/input notes when relevant.
Common gotchas.

## Constructors
| Name | Description |
| --- | --- |

## Fields
| Name | Description |
| --- | --- |

## Properties
| Name | Description |
| --- | --- |

## Methods
| Name | Description |
| --- | --- |

## Events
| Name | Description |
| --- | --- |

## Explicit Interface Implementations
| Name | Description |
| --- | --- |

## Applies to
Project/package/runtime context.

## See also
- Related class
- Related overview
- Related sample
````

## Member Page

````md
# Owner.Member Property

## Definition
Namespace: `...`
Assembly/Project: `...`
Source: `path/to/Owner.cs`

One-sentence summary.

```csharp
public PropertyType Member { get; set; }
```

### Property Value
`PropertyType`

Default value and meaning, if known.

## Examples
Minimal example.

## Remarks
Behavior, validation, invalidation, binding, layout, rendering, or input effects.

### Property Information
| Item | Value |
| --- | --- |
| Identifier field | `MemberProperty` |
| Default value | `...` |
| Metadata/options | `...` |

### XAML/Markup Usage
```xml
<object Member="value" />
```

## Applies to

## See also
````

For events, replace `Property Value` with `Event Type` and use `Routed Event Information` when applicable:

| Item | Value |
| --- | --- |
| Identifier field | `ClickEvent` |
| Routing strategy | `Bubble`, `Tunnel`, or `Direct` |
| Delegate/type | `...` |

For methods, include parameters, return value, exceptions, examples, and remarks. Keep overloads in one table on the class page; use separate member pages only when the method needs explanation.

## Adaptation Rules

- Keep `Definition`, `Examples`, and `Remarks` near the top.
- Put exhaustive member lists after the conceptual explanation.
- Mark inherited members as inherited only when listed.
- Prefer short descriptions in member tables.
- Omit sections that are genuinely empty.
- Add framework-specific metadata for dependency/UI properties, routed events, templates, visual states, layout, rendering, input, accessibility, and resources when the source supports it.
