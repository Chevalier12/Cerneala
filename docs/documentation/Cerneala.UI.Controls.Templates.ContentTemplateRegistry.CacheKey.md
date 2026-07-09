# ContentTemplateRegistry.CacheKey Record

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/ContentTemplateRegistry.cs`

Provides the private lookup key used by `ContentTemplateRegistry` to cache successful content-template resolutions.

```csharp
private sealed record CacheKey(string? RequestedKey, Type? DataType)
```

Containing type:
`ContentTemplateRegistry`

## Examples

`CacheKey` is private to `ContentTemplateRegistry`; the observable behavior is that repeated resolves for the same requested key and data type can reuse a cached template when every registered template is predicate-free.

```csharp
ContentTemplateRegistry registry = new();
registry.Register(new ContentTemplate<string>(
    "Text",
    key: "compact",
    priority: 0,
    context => new TextBlock { Text = context.Data ?? string.Empty }));

ContentTemplateMatchContext context = new("Ada", requestedKey: "compact");

registry.TryResolve(context, out ContentTemplate first);
registry.TryResolve(context, out ContentTemplate second);

bool sameTemplate = ReferenceEquals(first, second);
int cacheHits = registry.CacheHits;
```

## Remarks

`CacheKey` is an implementation detail of `ContentTemplateRegistry.TryResolve`. The registry builds a key from `ContentTemplateMatchContext.RequestedKey` and `ContentTemplateMatchContext.DataType` before checking its internal cache.

The key intentionally does not include the full match context. The registry only uses the cache when no registered template has a predicate, because predicate-based templates can depend on context beyond requested key and data type. Registering a template or unregistering an existing template clears the cache, so keys do not outlive registry mutations.

For `null` content, `DataType` is `null`. For unkeyed resolution, `RequestedKey` is `null`. Because `CacheKey` is a record, equality and hashing are based on those two values.

## Constructors

| Name | Description |
| --- | --- |
| `CacheKey(string? RequestedKey, Type? DataType)` | Initializes a cache key for a requested template key and runtime data type. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `RequestedKey` | `string?` | Gets the requested content-template key, or `null` for unkeyed resolution. |
| `DataType` | `Type?` | Gets the runtime type of the content data, or `null` when the content is `null`. |

## Applies to

Project: `Cerneala`

UI area: content-template registry internals.

## See also

- `Cerneala.UI.Controls.Templates.ContentTemplateRegistry`
- `Cerneala.UI.Controls.Templates.ContentTemplateMatchContext`
- `Cerneala.UI.Controls.Templates.ContentTemplate`
