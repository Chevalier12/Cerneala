namespace Cerneala.Tests.VisualStudio;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cerneala.VisualStudio.Preview;

public sealed class PreviewChromeTests
{
    [Fact]
    public void PreviewButtonsUseReadableDarkHoverChrome()
    {
        RunOnStaThread(() =>
        {
            Button button = CernealaPreviewChrome.Button("Split", "Show split view", 62);
            ControlTemplate template = Assert.IsType<ControlTemplate>(button.Template);
            Trigger hoverTrigger = Assert.Single(
                template.Triggers.OfType<Trigger>(),
                trigger => trigger.Property == UIElement.IsMouseOverProperty && Equals(trigger.Value, true));
            Setter backgroundSetter = Assert.Single(
                hoverTrigger.Setters.OfType<Setter>(),
                setter => setter.Property == Border.BackgroundProperty);
            SolidColorBrush hoverBackground = Assert.IsType<SolidColorBrush>(backgroundSetter.Value);
            SolidColorBrush foreground = Assert.IsType<SolidColorBrush>(button.Foreground);

            Assert.True(
                ContrastRatio(hoverBackground.Color, foreground.Color) >= 4.5,
                "Preview button hover text must remain readable against its background.");
            Assert.True(
                hoverBackground.Color.R < 100 && hoverBackground.Color.G < 100 && hoverBackground.Color.B < 100,
                "Preview button hover must stay inside the dark toolbar palette.");
            return true;
        });
    }

    [Fact]
    public void CompactInputsFitTheToolbarAndCenterTheirText()
    {
        (double TextBoxHeight, VerticalAlignment TextBoxAlignment, VerticalAlignment TextAlignment,
            double ComboBoxHeight, VerticalAlignment ComboBoxAlignment, VerticalAlignment EditorAlignment) result =
            RunOnStaThread(() =>
            {
                TextBox textBox = new();
                CernealaPreviewChrome.ConfigureTextBox(textBox, 50);
                ComboBox comboBox = new();
                CernealaPreviewChrome.ConfigureComboBox(comboBox, 66);
                comboBox.ApplyTemplate();
                TextBox editor = (TextBox)comboBox.Template.FindName("PART_EditableTextBox", comboBox);
                return (
                    textBox.Height + textBox.Margin.Top + textBox.Margin.Bottom,
                    textBox.VerticalAlignment,
                    textBox.VerticalContentAlignment,
                    comboBox.Height + comboBox.Margin.Top + comboBox.Margin.Bottom,
                    comboBox.VerticalAlignment,
                    editor.VerticalContentAlignment);
            });

        Assert.True(result.TextBoxHeight <= 30, $"Compact text box occupies {result.TextBoxHeight}px in a 30px toolbar content row.");
        Assert.Equal(VerticalAlignment.Center, result.TextBoxAlignment);
        Assert.Equal(VerticalAlignment.Center, result.TextAlignment);
        Assert.True(result.ComboBoxHeight <= 30, $"Compact combo box occupies {result.ComboBoxHeight}px in a 30px toolbar content row.");
        Assert.Equal(VerticalAlignment.Center, result.ComboBoxAlignment);
        Assert.Equal(VerticalAlignment.Center, result.EditorAlignment);
    }

    private static T RunOnStaThread<T>(Func<T> action)
    {
        T? result = default;
        Exception? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            throw failure;
        }

        return result!;
    }

    private static double ContrastRatio(Color first, Color second)
    {
        double firstLuminance = RelativeLuminance(first);
        double secondLuminance = RelativeLuminance(second);
        return (Math.Max(firstLuminance, secondLuminance) + 0.05) /
            (Math.Min(firstLuminance, secondLuminance) + 0.05);
    }

    private static double RelativeLuminance(Color color) =>
        (0.2126 * Linearize(color.R)) +
        (0.7152 * Linearize(color.G)) +
        (0.0722 * Linearize(color.B));

    private static double Linearize(byte channel)
    {
        double value = channel / 255d;
        return value <= 0.04045
            ? value / 12.92
            : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
