using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls;

[TemplatePart("PART_CheckMark", typeof(UIElement))]
public class RadioButton : ToggleButton
{
    public static readonly UiProperty<string> GroupNameProperty = UiProperty<string>.Register(
        nameof(GroupName),
        typeof(RadioButton),
        new UiPropertyMetadata<string>(string.Empty, UiPropertyOptions.None, validateValue: value => value is not null));

    private static readonly Brush DefaultBorderBrush = new SolidColorBrush(new Color(100, 110, 125));
    private UIElement? checkMark;

    // WPF's RadioButton style is not based on the ButtonBase style, so its content keeps Control's Left and Top.
    public RadioButton()
    {
        SetFrameworkDefault(ComponentTemplateProperty, RadioButtonTemplates.Default);
        SetFrameworkDefault(BorderBrushProperty, DefaultBorderBrush);
        SetFrameworkDefault(BorderThicknessProperty, new Thickness(1));
        SetFrameworkDefault(HorizontalContentAlignmentProperty, HorizontalAlignment.Left);
        SetFrameworkDefault(VerticalContentAlignmentProperty, VerticalAlignment.Top);
        Handlers.AddHandler(InputEvents.KeyDownEvent, OnKeyDown);
    }

    // The name of the group this radio button belongs to. With a name, the group is every radio button with that name
    // under the same visual root. Without a name, it is the unnamed radio buttons that share this one's parent.
    public string GroupName
    {
        get => GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        ApplyTemplate();
        SynchronizeIndicator();
        return base.MeasureCore(context);
    }

    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        checkMark = instance is null
            ? null
            : GetOptionalTemplatePart<UIElement>("PART_CheckMark");
        SynchronizeIndicator();
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, IsCheckedProperty) ||
            ReferenceEquals(args.Property, BorderThicknessProperty))
        {
            SynchronizeIndicator();
        }
    }

    // The dot is shown while the radio button is checked. The ring is the default template's own round indicator; its
    // stroke thickness is the largest side of BorderThickness, as Button draws its border.
    private void SynchronizeIndicator()
    {
        if (checkMark is null)
        {
            return;
        }

        checkMark.Visibility = IsChecked ? Visibility.Visible : Visibility.Hidden;
        if (checkMark.VisualParent is RadioButtonTemplates.RoundIndicator indicator)
        {
            Thickness border = BorderThickness;
            indicator.Ring.StrokeThickness = MathF.Max(
                MathF.Max(border.Left, border.Top),
                MathF.Max(border.Right, border.Bottom));
        }
    }

    // Reached only through ButtonBase's click, so a release that a command source in the content owns, or a release
    // that follows no press on this button, leaves IsChecked unchanged (#587). A click never unchecks.
    protected override void OnToggle()
    {
        IsChecked = true;
    }

    // As WPF, the other checked radio buttons of the group are unchecked before Checked is raised, so their Unchecked
    // comes first.
    protected override void OnChecked(RoutedEventArgs e)
    {
        foreach (RadioButton other in GetGroup())
        {
            if (!ReferenceEquals(other, this) && other.IsChecked)
            {
                other.IsChecked = false;
            }
        }

        base.OnChecked(e);
    }

    private void OnKeyDown(UiElementId _, RoutedEventArgs args)
    {
        if (args.Handled || args is not KeyEventArgs keyArgs)
        {
            return;
        }

        int step = keyArgs.Key switch
        {
            InputKey.Up or InputKey.Left => -1,
            InputKey.Down or InputKey.Right => 1,
            _ => 0
        };
        if (step == 0 ||
            Root is not UIRoot { ActiveFocusManager: { } focusManager } root ||
            !ReferenceEquals(focusManager.FocusedElement, this))
        {
            return;
        }

        List<RadioButton> group = GetGroup();
        int index = group.IndexOf(this);
        if (index < 0)
        {
            return;
        }

        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        for (int distance = 1; distance < group.Count; distance++)
        {
            int candidateIndex = (((index + (step * distance)) % group.Count) + group.Count) % group.Count;
            RadioButton candidate = group[candidateIndex];
            if (!FocusPolicy.CanFocus(candidate, routeMap))
            {
                continue;
            }

            if (focusManager.Focus(candidate, routeMap))
            {
                candidate.IsChecked = true;
                args.Handled = true;
            }

            return;
        }
    }

    // The group of this radio button in visual pre-order, this one included. A named group is every radio button with
    // that name under the topmost visual ancestor. An unnamed group is the unnamed radio buttons among the logical
    // children of the logical parent, or of the visual parent when there is no logical parent.
    private List<RadioButton> GetGroup()
    {
        string groupName = GroupName;
        IEnumerable<UIElement> candidates;
        if (groupName.Length > 0)
        {
            UIElement top = this;
            while (top.VisualParent is { } parent)
            {
                top = parent;
            }

            candidates = ElementTreeWalker.PreOrder(top);
        }
        else if (LogicalParent is { } logicalParent)
        {
            candidates = logicalParent.LogicalChildren;
        }
        else if (VisualParent is { } visualParent)
        {
            candidates = visualParent.VisualChildren;
        }
        else
        {
            candidates = [this];
        }

        List<RadioButton> group = [];
        foreach (UIElement candidate in candidates)
        {
            if (candidate is RadioButton radioButton &&
                string.Equals(radioButton.GroupName, groupName, StringComparison.Ordinal))
            {
                group.Add(radioButton);
            }
        }

        return group;
    }
}
