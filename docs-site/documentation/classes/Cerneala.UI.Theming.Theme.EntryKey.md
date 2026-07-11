# Theme.EntryKey Record

## Definition
Namespace: `Cerneala.UI.Theming`

Assembly/Project: `Cerneala`

Source: `UI/Theming/Theme.cs`

Provides the private dictionary key used by `Theme` to store values by both theme key text and value type.

```csharp
private readonly record struct EntryKey(Type ValueType, string Key)
```

Containing type:
`Theme`

## Examples

`Theme.Set<T>` stores entries by converting a typed theme key into an `EntryKey`:

```csharp
ThemeKey<Color> accentKey = new("Accent");
Theme theme = new Theme("Editor").Set(accentKey, Color.White);
```

Internally, the stored key is equivalent to:

```csharp
EntryKey entryKey = EntryKey.From(accentKey);
```

## Remarks

`EntryKey` is a private nested implementation detail of `Theme`. It combines `typeof(T)` with `ThemeKey<T>.Key`, so theme entries with the same string key but different value types occupy different dictionary slots.

`Set<T>`, `TryGet<T>`, and `Get<T>` all use `EntryKey.From<T>` before accessing the theme's internal dictionary. This keeps lookup behavior typed while allowing the dictionary to store values as `object?`.

Because `EntryKey` is a readonly record struct, equality and hashing are value based across its `ValueType` and `Key` components.

## Constructors

| Name | Description |
| --- | --- |
| `EntryKey(Type, string)` | Initializes an internal theme entry key from a value type and theme key string. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ValueType` | `Type` | Gets the value type associated with the typed theme key. |
| `Key` | `string` | Gets the theme key string from `ThemeKey<T>.Key`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `From<T>(ThemeKey<T> key)` | `EntryKey` | Creates an entry key from a typed theme key using `typeof(T)` and `key.Key`. |

## Applies to

Internal storage for `Cerneala.UI.Theming.Theme` in the `Cerneala` project.

## See also

- `Cerneala.UI.Theming.Theme`
- `Cerneala.UI.Theming.ThemeKey<T>`
- `Cerneala.UI.Theming.DefaultTheme`
