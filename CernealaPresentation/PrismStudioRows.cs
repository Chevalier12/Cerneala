using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.UI.Input;

namespace Cerneala.Presentation;

internal abstract class PrismStudioRowModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

internal sealed class PrismStudioCommand(
    Action<object?> execute,
    Func<object?, bool>? canExecute = null) : ICommand
{
    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => execute(parameter);
}

internal sealed class PrismStudioCatalogRow : PrismStudioRowModel
{
    public PrismStudioCatalogRow(string text, bool isEnabled, Action execute)
    {
        Content = text;
        Command = new PrismStudioCommand(_ => execute(), _ => isEnabled);
    }

    public object Content { get; }

    public ICommand Command { get; }
}

internal sealed class PrismStudioLayerRow : PrismStudioRowModel
{
    private readonly Action<bool> visibilityChanged;
    private bool isVisible;

    public PrismStudioLayerRow(
        string name,
        bool isSelected,
        bool isVisible,
        IReadOnlyList<PrismStudioOperationRow> filters,
        IReadOnlyList<PrismStudioOperationRow> styles,
        Action select,
        Action moveUp,
        Action moveDown,
        Action<bool> visibilityChanged)
    {
        Content = name;
        IsSelected = isSelected;
        this.isVisible = isVisible;
        Filters = filters;
        Styles = styles;
        SelectCommand = new PrismStudioCommand(_ => select());
        MoveUpCommand = new PrismStudioCommand(_ => moveUp());
        MoveDownCommand = new PrismStudioCommand(_ => moveDown());
        this.visibilityChanged = visibilityChanged;
    }

    public object Content { get; }

    public bool IsSelected { get; }

    public bool IsVisible
    {
        get => isVisible;
        set
        {
            if (SetField(ref isVisible, value))
            {
                visibilityChanged(value);
            }
        }
    }

    public IEnumerable Filters { get; }

    public IEnumerable Styles { get; }

    public ICommand SelectCommand { get; }

    public ICommand MoveUpCommand { get; }

    public ICommand MoveDownCommand { get; }
}

internal sealed class PrismStudioOperationRow : PrismStudioRowModel
{
    private readonly Action<bool> visibilityChanged;
    private bool isVisible;

    public PrismStudioOperationRow(
        string name,
        bool isSelected,
        bool isVisible,
        Action select,
        Action moveUp,
        Action moveDown,
        Action<bool> visibilityChanged)
    {
        Content = name;
        IsSelected = isSelected;
        this.isVisible = isVisible;
        SelectCommand = new PrismStudioCommand(_ => select());
        MoveUpCommand = new PrismStudioCommand(_ => moveUp());
        MoveDownCommand = new PrismStudioCommand(_ => moveDown());
        this.visibilityChanged = visibilityChanged;
    }

    public object Content { get; }

    public bool IsSelected { get; }

    public bool IsVisible
    {
        get => isVisible;
        set
        {
            if (SetField(ref isVisible, value))
            {
                visibilityChanged(value);
            }
        }
    }

    public ICommand SelectCommand { get; }

    public ICommand MoveUpCommand { get; }

    public ICommand MoveDownCommand { get; }
}

internal abstract class PrismStudioLabeledRow(string label) : PrismStudioRowModel
{
    public string Label { get; } = label;
}

internal sealed class PrismStudioLayerTitleRow(string label) : PrismStudioLabeledRow(label);

internal sealed class PrismStudioOperationTitleRow(string label) : PrismStudioLabeledRow(label);

internal sealed class PrismStudioChoiceRow : PrismStudioLabeledRow
{
    private readonly IReadOnlyList<object?> items;
    private readonly Action<object?> changed;
    private int selectedIndex;

    public PrismStudioChoiceRow(
        string label,
        IReadOnlyList<object?> items,
        int selectedIndex,
        Action<object?> changed)
        : base(label)
    {
        this.items = items;
        this.selectedIndex = selectedIndex;
        this.changed = changed;
    }

    public IEnumerable Items => items;

    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            if (!SetField(ref selectedIndex, value) || value < 0 || value >= items.Count)
            {
                return;
            }

            changed(items[value]);
        }
    }
}

internal sealed class PrismStudioBooleanRow : PrismStudioLabeledRow
{
    private readonly Action<bool> changed;
    private bool isChecked;

    public PrismStudioBooleanRow(string label, bool isChecked, Action<bool> changed)
        : base(label)
    {
        this.isChecked = isChecked;
        this.changed = changed;
    }

    public bool IsChecked
    {
        get => isChecked;
        set
        {
            if (SetField(ref isChecked, value))
            {
                changed(value);
            }
        }
    }
}

internal sealed class PrismStudioUnitSliderRow : PrismStudioLabeledRow
{
    private readonly Action<float> changed;
    private float value;

    public PrismStudioUnitSliderRow(string label, float value, Action<float> changed)
        : base(label)
    {
        this.value = value;
        this.changed = changed;
    }

    public float Value
    {
        get => value;
        set
        {
            if (SetField(ref this.value, value))
            {
                changed(value);
            }
        }
    }
}

internal sealed class PrismStudioFiniteNumberRow : PrismStudioLabeledRow
{
    private readonly Action<float> changed;
    private readonly float minimum;
    private readonly float maximum;
    private float value;
    private string text;
    private bool synchronizing;

    public PrismStudioFiniteNumberRow(
        string label,
        float value,
        float minimum,
        float maximum,
        Action<float> changed)
        : base(label)
    {
        this.value = value;
        this.minimum = minimum;
        this.maximum = maximum;
        this.changed = changed;
        text = Format(value);
    }

    public float Minimum => minimum;

    public float Maximum => maximum;

    public float Value
    {
        get => value;
        set
        {
            if (!SetField(ref this.value, value) || synchronizing)
            {
                return;
            }

            synchronizing = true;
            Text = Format(value);
            synchronizing = false;
            changed(value);
        }
    }

    public string Text
    {
        get => text;
        set
        {
            if (!SetField(ref text, value ?? string.Empty) || synchronizing ||
                !float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ||
                parsed < minimum || parsed > maximum)
            {
                return;
            }

            synchronizing = true;
            Value = parsed;
            synchronizing = false;
            changed(parsed);
        }
    }

    private static string Format(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}

internal sealed class PrismStudioStepperRow : PrismStudioLabeledRow
{
    private readonly Action<object> changed;
    private readonly bool integer;
    private double fallback;
    private string text;
    private bool synchronizing;

    public PrismStudioStepperRow(string label, double value, bool integer, Action<object> changed)
        : base(label)
    {
        fallback = value;
        this.integer = integer;
        this.changed = changed;
        text = value.ToString("0.###", CultureInfo.InvariantCulture);
        DecreaseCommand = new PrismStudioCommand(_ => Step(-1));
        IncreaseCommand = new PrismStudioCommand(_ => Step(1));
    }

    public string Text
    {
        get => text;
        set
        {
            if (!SetField(ref text, value ?? string.Empty) || synchronizing ||
                !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                return;
            }

            Commit(parsed, synchronizeText: false);
        }
    }

    public ICommand DecreaseCommand { get; }

    public ICommand IncreaseCommand { get; }

    private void Step(double delta)
    {
        double current = double.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
            ? parsed
            : fallback;
        Commit(current + delta, synchronizeText: true);
    }

    private void Commit(double value, bool synchronizeText)
    {
        object typed = integer ? checked((int)Math.Round(value)) : (float)value;
        try
        {
            changed(typed);
            fallback = Convert.ToDouble(typed, CultureInfo.InvariantCulture);
            if (synchronizeText)
            {
                synchronizing = true;
                Text = Convert.ToString(typed, CultureInfo.InvariantCulture) ?? "0";
                synchronizing = false;
            }
        }
        catch (ArgumentOutOfRangeException)
        {
        }
    }
}

internal sealed class PrismStudioColorRow : PrismStudioLabeledRow
{
    private readonly Action<Color> changed;
    private string text;
    private Color selectedColor;
    private bool synchronizing;

    public PrismStudioColorRow(string label, Color color, Action<Color> changed)
        : base(label)
    {
        selectedColor = color;
        text = Format(color);
        this.changed = changed;
        SetColorCommand = new PrismStudioCommand(SetColor);
    }

    public string Text
    {
        get => text;
        set
        {
            if (!SetField(ref text, value ?? string.Empty) || synchronizing || !Color.TryParse(text, out Color parsed))
            {
                return;
            }

            synchronizing = true;
            SelectedColor = parsed;
            synchronizing = false;
            changed(parsed);
        }
    }

    public Color SelectedColor
    {
        get => selectedColor;
        set
        {
            if (!SetField(ref selectedColor, value) || synchronizing)
            {
                return;
            }

            synchronizing = true;
            Text = Format(value);
            synchronizing = false;
            changed(value);
        }
    }

    public ICommand SetColorCommand { get; }

    private void SetColor(object? parameter)
    {
        if (parameter is Color color || parameter is string textValue && Color.TryParse(textValue, out color))
        {
            SelectedColor = color;
        }
    }

    private static string Format(Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
}

internal sealed class PrismStudioVectorRow : PrismStudioLabeledRow
{
    public PrismStudioVectorRow(string label, Vector4 value, Action<Vector4> changed)
        : base(label)
    {
        float[] values = [value.X, value.Y, value.Z, value.W];
        Components = Enumerable.Range(0, values.Length)
            .Select(index => new PrismStudioVectorComponentRow(values[index], componentValue =>
            {
                values[index] = componentValue;
                changed(new Vector4(values[0], values[1], values[2], values[3]));
            }))
            .ToArray();
    }

    public IEnumerable Components { get; }
}

internal sealed class PrismStudioVectorComponentRow : PrismStudioRowModel
{
    private readonly Action<float> changed;
    private string text;

    public PrismStudioVectorComponentRow(float value, Action<float> changed)
    {
        text = value.ToString("0.###", CultureInfo.InvariantCulture);
        this.changed = changed;
    }

    public string Text
    {
        get => text;
        set
        {
            if (SetField(ref text, value ?? string.Empty) &&
                float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                changed(parsed);
            }
        }
    }
}

internal sealed class PrismStudioResourceRow(string label) : PrismStudioLabeledRow(label);

internal sealed class PrismStudioOperationActionsRow : PrismStudioRowModel
{
    public PrismStudioOperationActionsRow(Action moveUp, Action moveDown, Action delete)
    {
        MoveUpCommand = new PrismStudioCommand(_ => moveUp());
        MoveDownCommand = new PrismStudioCommand(_ => moveDown());
        DeleteCommand = new PrismStudioCommand(_ => delete());
    }

    public ICommand MoveUpCommand { get; }

    public ICommand MoveDownCommand { get; }

    public ICommand DeleteCommand { get; }
}
