# GridLength Struct

## Definition
Namespace: `Cerneala.UI.Layout.Panels`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Panels/GridLength.cs`

Represents the sizing mode and numeric value for a `Grid` row or column definition.

```csharp
public readonly record struct GridLength(float Value, GridUnitType UnitType)
```

Inheritance:
`Object` -> `ValueType` -> `GridLength`

Implements:
`IEquatable<GridLength>`

## Examples

Create fixed, auto, and proportional grid definitions.

```csharp
using Cerneala.UI.Layout.Panels;

Grid grid = new();
grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(96)));
grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(2)));

grid.RowDefinitions.Add(new RowDefinition(new GridLength(32)));
grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
```

Validate a value before accepting it from custom code.

```csharp
using Cerneala.UI.Layout.Panels;

GridLength length = GridLength.Stars(1);
length.Validate();
```

## Remarks

`GridLength` is used by `ColumnDefinition.Width` and `RowDefinition.Height` to describe how `Grid` resolves each column or row.

Pixel lengths use `Value` as a fixed layout size. Auto lengths are sized from measured child content in the corresponding row or column. Star lengths divide remaining finite space proportionally by their `Value`; `GridLength.Star` is equivalent to one star.

The primary constructor and factory methods store the supplied values directly. Call `Validate()` when a value must be accepted by grid layout. `ColumnDefinition` and `RowDefinition` call `Validate()` from their size property setters. Validation requires a valid `GridUnitType` and a finite, non-negative `Value`.

Because `GridLength` is a `readonly record struct`, values are immutable after construction and use value-based equality. The compiler provides record-struct members such as equality, deconstruction, hashing, and string formatting based on `Value` and `UnitType`.

## Constructors

| Name | Description |
| --- | --- |
| `GridLength(float Value, GridUnitType UnitType)` | Initializes a grid length with the specified value and unit type. |
| `GridLength(float value)` | Initializes a pixel grid length with the specified value. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Value` | `float` | Gets the numeric value for the length. Pixel lengths use it as the fixed size; star lengths use it as the proportional weight. |
| `UnitType` | `GridUnitType` | Gets the unit type that determines how `Grid` interprets `Value`. |
| `Auto` | `GridLength` | Gets an auto-sized length with `Value` set to `1` and `UnitType` set to `GridUnitType.Auto`. |
| `Star` | `GridLength` | Gets a one-star length with `Value` set to `1` and `UnitType` set to `GridUnitType.Star`. |
| `IsAuto` | `bool` | Gets whether `UnitType` is `GridUnitType.Auto`. |
| `IsPixel` | `bool` | Gets whether `UnitType` is `GridUnitType.Pixel`. |
| `IsStar` | `bool` | Gets whether `UnitType` is `GridUnitType.Star`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Pixels(float value)` | `GridLength` | Creates a pixel grid length with the specified value. |
| `Stars(float value)` | `GridLength` | Creates a star grid length with the specified proportional value. |
| `Validate()` | `void` | Throws when `UnitType` is outside the defined `GridUnitType` range, or when `Value` is negative, `NaN`, or infinite. |
| `Deconstruct(out float Value, out GridUnitType UnitType)` | `void` | Deconstructs the length into its value and unit type. |
| `Equals(GridLength other)` | `bool` | Determines whether another `GridLength` has the same value and unit type. |
| `GetHashCode()` | `int` | Returns a hash code based on `Value` and `UnitType`. |
| `ToString()` | `string` | Returns the compiler-generated record string representation. |

## Unit Types

| Name | Description |
| --- | --- |
| `GridUnitType.Pixel` | Uses `Value` as a fixed layout size. |
| `GridUnitType.Auto` | Sizes the row or column from measured child content. |
| `GridUnitType.Star` | Uses `Value` as a proportional weight for remaining finite space. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Validate()` | `ArgumentOutOfRangeException` | `UnitType` is not `Pixel`, `Auto`, or `Star`. |
| `Validate()` | `ArgumentOutOfRangeException` | `Value` is negative, `NaN`, positive infinity, or negative infinity. |

## Applies To

Cerneala retained UI grid layout in the `Cerneala.UI.Layout.Panels` namespace.

## See Also

- `Cerneala.UI.Layout.Panels.Grid`
- `Cerneala.UI.Layout.Panels.ColumnDefinition`
- `Cerneala.UI.Layout.Panels.RowDefinition`
