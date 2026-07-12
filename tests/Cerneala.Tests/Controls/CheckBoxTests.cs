using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using CheckMarkPath = Cerneala.UI.Controls.Shapes.Path;

namespace Cerneala.Tests.Controls;

public sealed class CheckBoxTests
{
    [Fact]
    public void CheckBoxMeasuresBoxAndTextContent()
    {
        CheckBox checkBox = new() { Content = "Agree", FontSize = 10 };

        LayoutSize size = checkBox.Measure(new MeasureContext(new LayoutSize(200, 100)));

        Assert.True(size.Width > 20);
        Assert.True(size.Height >= 10);
    }

    [Fact]
    public void MarkupBrushesOverrideCheckBoxDefaults()
    {
        CheckBox checkBox = new();
        SolidColorBrush foreground = new(Color.White);
        SolidColorBrush background = new(Color.Tomato);
        SolidColorBrush borderBrush = new(Color.Black);

        checkBox.SetValue(Control.ForegroundProperty, foreground, UiPropertyValueSource.MarkupBase);
        checkBox.SetValue(Control.BackgroundProperty, background, UiPropertyValueSource.MarkupBase);
        checkBox.SetValue(Control.BorderBrushProperty, borderBrush, UiPropertyValueSource.MarkupBase);

        Assert.Same(foreground, checkBox.Foreground);
        Assert.Same(background, checkBox.Background);
        Assert.Same(borderBrush, checkBox.BorderBrush);
    }

    [Fact]
    public void CheckBoxTemplateSeparatesBackgroundTextAndCheckMarkBrushes()
    {
        UIRoot root = new();
        SolidColorBrush foreground = new(Color.White);
        SolidColorBrush background = new(Color.Black);
        CheckBox checkBox = new()
        {
            Content = "Agree",
            IsChecked = true,
            Foreground = foreground,
            Background = background
        };
        root.VisualChildren.Add(checkBox);
        checkBox.Measure(new MeasureContext(new LayoutSize(200, 100)));
        checkBox.Arrange(new ArrangeContext(new LayoutRect(20, 30, 80, 20)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        ComponentTemplateInstance instance = Assert.IsType<ComponentTemplateInstance>(checkBox.ComponentTemplateInstance);
        Assert.True(instance.Parts.TryGetValue("PART_CheckMark", out UIElement? element));
        CheckMarkPath checkMark = Assert.IsType<CheckMarkPath>(element);
        UIElement indicator = Assert.IsAssignableFrom<UIElement>(checkMark.VisualParent);
        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Equal(indicator.ArrangedBounds.Width, indicator.ArrangedBounds.Height);
        float checkBoxCenterY = checkBox.ArrangedBounds.Y + (checkBox.ArrangedBounds.Height / 2);
        float indicatorCenterY = indicator.ArrangedBounds.Y + (indicator.ArrangedBounds.Height / 2);
        Assert.InRange(MathF.Abs(checkBoxCenterY - indicatorCenterY), 0, 0.5f);
        Assert.Contains(commands, command => command.Kind == DrawCommandKind.FillRectangle && ReferenceEquals(command.Brush, background));
        Assert.Single(commands, command => command.Kind == DrawCommandKind.FillPath && ReferenceEquals(command.Brush, checkMark.Fill));
        Assert.Equal(Color.Black, Assert.IsType<SolidColorBrush>(checkMark.Fill).Color);
        Assert.NotSame(foreground, checkMark.Fill);
        Assert.All(
            commands.Where(command => command.Kind == DrawCommandKind.FillPath),
            command => Assert.True(command.Rect.X > 0));
        Assert.Contains(commands, command => command.Kind == DrawCommandKind.DrawText && ReferenceEquals(command.Brush, foreground));
    }

    [Fact]
    public void DefaultTemplateProvidesPathCheckMarkPart()
    {
        CheckBox checkBox = new();

        checkBox.Measure(new MeasureContext(new LayoutSize(100, 30)));

        ComponentTemplateInstance instance = Assert.IsType<ComponentTemplateInstance>(checkBox.ComponentTemplateInstance);
        Assert.True(instance.Parts.TryGetValue("PART_CheckMark", out UIElement? element));
        CheckMarkPath checkMark = Assert.IsType<CheckMarkPath>(element);
        SvgGeometry geometry = Assert.IsType<SvgGeometry>(checkMark.Geometry);
        Assert.Equal(CheckBoxTemplates.DefaultCheckMarkData, geometry.Data);
        Assert.Equal(new DrawRect(0, 0, 100, 100), geometry.Bounds);
        Assert.Equal(Color.Black, Assert.IsType<SolidColorBrush>(checkMark.Fill).Color);
        Assert.Equal(Visibility.Hidden, checkMark.Visibility);

        ((IInputActivatable)checkBox).Activate();

        Assert.Equal(Visibility.Visible, checkMark.Visibility);
    }

    [Fact]
    public void CheckMarkStretchesUniformlyAndStaysCenteredInLargerTemplateBox()
    {
        CheckMarkPath? checkMark = null;
        ComponentTemplate<CheckBox> template = new("CheckBox.LargeIndicator", context =>
        {
            checkMark = new CheckMarkPath
            {
                Data = new PathGeometry(
                [
                    new DrawPoint(0, 4),
                    new DrawPoint(3, 7),
                    new DrawPoint(8, 0)
                ]),
                Stroke = new SolidColorBrush(Color.Black),
                StrokeThickness = 2
            };
            context.RequirePart("PART_CheckMark", checkMark);
            return new Border { Child = checkMark, Padding = new Thickness(5) };
        });
        CheckBox checkBox = new() { ComponentTemplate = template, IsChecked = true };

        checkBox.Measure(new MeasureContext(new LayoutSize(40, 40)));
        checkBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));

        CheckMarkPath part = Assert.IsType<CheckMarkPath>(checkMark);
        PathGeometry geometry = Assert.IsType<PathGeometry>(part.Data);
        Matrix3x2 matrix = part.RenderTransform.Matrix;
        DrawPoint geometryCenter = new(
            geometry.Bounds.X + (geometry.Bounds.Width / 2),
            geometry.Bounds.Y + (geometry.Bounds.Height / 2));
        DrawPoint renderedCenter = matrix.Transform(geometryCenter);
        LayoutRect bounds = part.ArrangedBounds;

        Assert.True(matrix.M11 > 1);
        Assert.Equal(matrix.M11, matrix.M22);
        Assert.Equal(bounds.X + (bounds.Width / 2), renderedCenter.X, 3);
        Assert.Equal(bounds.Y + (bounds.Height / 2), renderedCenter.Y, 3);
    }

    [Fact]
    public void CheckBoxActivationTogglesCheckedState()
    {
        CheckBox checkBox = new();

        ((IInputActivatable)checkBox).Activate();

        Assert.True(checkBox.IsChecked);
    }
}
