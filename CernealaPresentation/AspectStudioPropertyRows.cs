using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Media;

namespace Cerneala.Presentation;

internal abstract class AspectStudioPropertyRowModel : INotifyPropertyChanged
{
    protected AspectStudioPropertyRowModel(string label, Brush labelBrush)
    {
        Label = label;
        LabelBrush = labelBrush;
        AutomationId = $"aspect-property-{label}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Label { get; }

    public Brush LabelBrush { get; }

    public string AutomationId { get; }

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

    protected void RaisePropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

internal sealed class AspectStudioHeaderRow : AspectStudioPropertyRowModel
{
    public AspectStudioHeaderRow(string label, Brush labelBrush)
        : base(label, labelBrush)
    {
    }
}

internal sealed class AspectStudioTextRow : AspectStudioPropertyRowModel
{
    private readonly Func<string, (bool Success, object? Value, string Error)> parse;
    private readonly Action<object?> commit;
    private readonly Action<string> reportError;
    private string text;
    private Brush borderBrush;

    public AspectStudioTextRow(
        string label,
        Brush labelBrush,
        string text,
        Brush borderBrush,
        Func<string, (bool Success, object? Value, string Error)> parse,
        Action<object?> commit,
        Action<string> reportError)
        : base(label, labelBrush)
    {
        this.text = text;
        this.borderBrush = borderBrush;
        this.parse = parse;
        this.commit = commit;
        this.reportError = reportError;
    }

    public string Text
    {
        get => text;
        set
        {
            if (!SetField(ref text, value ?? string.Empty))
            {
                return;
            }

            (bool success, object? parsed, string error) = parse(text);
            BorderBrush = success ? AspectChapterView.LineBrush : AspectChapterView.PinkBrush;
            if (success)
            {
                commit(parsed);
            }
            else
            {
                reportError(error);
            }
        }
    }

    public Brush BorderBrush
    {
        get => borderBrush;
        private set => SetField(ref borderBrush, value);
    }
}

internal sealed class AspectStudioBooleanRow : AspectStudioPropertyRowModel
{
    private readonly Action<object?> commit;
    private bool isChecked;

    public AspectStudioBooleanRow(string label, Brush labelBrush, bool isChecked, Action<object?> commit)
        : base(label, labelBrush)
    {
        this.isChecked = isChecked;
        this.commit = commit;
    }

    public bool IsChecked
    {
        get => isChecked;
        set
        {
            if (SetField(ref isChecked, value))
            {
                commit(value);
            }
        }
    }
}

internal sealed class AspectStudioChoiceRow : AspectStudioPropertyRowModel
{
    private readonly Func<object?, object?> convert;
    private readonly Action<object?> commit;
    private readonly IReadOnlyList<object?> items;
    private int selectedIndex;

    public AspectStudioChoiceRow(
        string label,
        Brush labelBrush,
        IReadOnlyList<object?> items,
        int selectedIndex,
        Func<object?, object?> convert,
        Action<object?> commit)
        : base(label, labelBrush)
    {
        this.items = items;
        this.selectedIndex = selectedIndex;
        this.convert = convert;
        this.commit = commit;
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

            commit(convert(items[value]));
        }
    }
}

internal sealed class AspectStudioColorRow : AspectStudioPropertyRowModel
{
    private readonly Func<string, (bool Success, object? Value, string Error)> parse;
    private readonly Action<object?> commit;
    private readonly Action<string> reportError;
    private string text;
    private Color selectedColor;
    private Brush borderBrush;
    private bool synchronizing;

    public AspectStudioColorRow(
        string label,
        Brush labelBrush,
        string text,
        Color selectedColor,
        Brush borderBrush,
        Func<string, (bool Success, object? Value, string Error)> parse,
        Action<object?> commit,
        Action<string> reportError)
        : base(label, labelBrush)
    {
        this.text = text;
        this.selectedColor = selectedColor;
        this.borderBrush = borderBrush;
        this.parse = parse;
        this.commit = commit;
        this.reportError = reportError;
    }

    public string Text
    {
        get => text;
        set
        {
            if (!SetField(ref text, value ?? string.Empty) || synchronizing)
            {
                return;
            }

            (bool success, object? parsed, string error) = parse(text);
            BorderBrush = success ? AspectChapterView.LineBrush : AspectChapterView.PinkBrush;
            if (!success)
            {
                reportError(error);
                return;
            }

            Color color = parsed is SolidColorBrush solid ? solid.Color : Color.Transparent;
            synchronizing = true;
            try
            {
                SelectedColor = color;
            }
            finally
            {
                synchronizing = false;
            }

            commit(parsed);
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

            SolidColorBrush brush = new(value);
            synchronizing = true;
            try
            {
                Text = AspectChapterView.FormatBrush(brush);
                BorderBrush = AspectChapterView.LineBrush;
            }
            finally
            {
                synchronizing = false;
            }

            commit(brush);
        }
    }

    public Brush BorderBrush
    {
        get => borderBrush;
        private set => SetField(ref borderBrush, value);
    }

    public string SwatchAutomationId => $"aspect-color-{Label}-swatch";
}
