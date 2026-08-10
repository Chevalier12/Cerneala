using Cerneala.UI.Automation;
using Cerneala.UI.Controls;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using System.Text.Json;

namespace Cerneala.ComboBoxLab;

public partial class MainWindow : Window
{
    private static readonly string[] StandardItems =
    [
        "Apple", "Application", "Pineapple", "Grape", "Apricot", "Maple", "Banana",
        "Alpine", "Alpha", "Berlin", "Bucharest", "Budapest", "Iasi", "Cluj-Napoca",
        "Timisoara", "Constanta", "Craiova", "Brasov", "Oradea", "Sibiu"
    ];

    private bool initialized;
    private bool captureRequested;
    private int captureSequence;
    private readonly string? captureDirectory = Environment.GetEnvironmentVariable("CERNEALA_COMBOBOX_LAB_CAPTURE_DIR");

    private void OnContentRendered(object? sender, EventArgs args)
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        ResetControls();
        AssignAutomationIds();
        captureRequested = true;
        Invalidate(InvalidationFlags.Render, "ComboBox lab baseline API capture");
    }

    private void OnFrameRendered(object? sender, EventArgs args)
    {
        if (!initialized)
        {
            return;
        }

        bool stateTextChanged =
            UpdateStateText(FilteredCombo, FilteredCommittedState, FilteredRuntimeState) |
            UpdateStateText(SelectionCombo, SelectionCommittedState, SelectionRuntimeState) |
            UpdateText(BottomState, StateSummary(BottomCombo));

        if (captureRequested)
        {
            if (stateTextChanged)
            {
                Invalidate(InvalidationFlags.Render, "ComboBox lab API capture settle");
                return;
            }

            captureRequested = false;
            CaptureAutomationState("manual");
        }
    }

    private void OnPreviewKeyDown(UiElementId sender, RoutedEventArgs args)
    {
        if (args is KeyEventArgs { Key: InputKey.F12 })
        {
            captureRequested = true;
            Invalidate(InvalidationFlags.Render, "ComboBox lab API capture");
            args.Handled = true;
        }
    }

    private void OnReset(UiElementId sender, RoutedEventArgs args)
    {
        ResetControls();
    }

    private void OnLoadStressItems(UiElementId sender, RoutedEventArgs args)
    {
        string[] stressItems =
        [
            .. StandardItems,
            .. Enumerable.Range(1, 180).Select(index => $"Generated item {index:000}")
        ];
        SetAllItems(stressItems);
    }

    private void OnFilterToggle(UiElementId sender, RoutedEventArgs args)
    {
        FilteredCombo.IsTextFilterEnabled = FilterToggle.IsChecked;
    }

    private void OnCaseToggle(UiElementId sender, RoutedEventArgs args)
    {
        FilteredCombo.IsTextSearchCaseSensitive = CaseToggle.IsChecked;
    }

    private void OnReadOnlyToggle(UiElementId sender, RoutedEventArgs args)
    {
        FilteredCombo.IsReadOnly = ReadOnlyToggle.IsChecked;
    }

    private void ResetControls()
    {
        FilterToggle.IsChecked = true;
        CaseToggle.IsChecked = false;
        ReadOnlyToggle.IsChecked = false;
        FilteredCombo.IsTextFilterEnabled = true;
        FilteredCombo.IsTextSearchCaseSensitive = false;
        FilteredCombo.IsReadOnly = false;
        SetAllItems(StandardItems);
        FilteredCombo.SelectedIndex = 4;
        SelectionCombo.SelectedIndex = 0;
        BottomCombo.SelectedIndex = 1;
    }

    private void SetAllItems(IEnumerable<string> items)
    {
        string[] materialized = items.ToArray();
        FilteredCombo.SetItems(materialized);
        SelectionCombo.SetItems(materialized);
        BottomCombo.SetItems(materialized);
    }

    private void AssignAutomationIds()
    {
        AutomationProperties.SetAutomationId(FilteredCombo, "filtered-combo");
        AutomationProperties.SetAutomationId(SelectionCombo, "selection-combo");
        AutomationProperties.SetAutomationId(BottomCombo, "bottom-combo");
    }

    private static bool UpdateStateText(ComboBox comboBox, TextBlock committed, TextBlock runtime)
    {
        return
            UpdateText(committed, $"COMMITTED  index={comboBox.SelectedIndex}  text=\"{comboBox.Text}\"") |
            UpdateText(runtime, $"open={comboBox.IsDropDownOpen}  realized={comboBox.ItemContainerGenerator.RealizedContainers.Count}");
    }

    private static bool UpdateText(TextBlock textBlock, string text)
    {
        if (textBlock.Text == text)
        {
            return false;
        }

        textBlock.Text = text;
        return true;
    }

    private static string StateSummary(ComboBox comboBox)
    {
        return $"index={comboBox.SelectedIndex}  text=\"{comboBox.Text}\"  open={comboBox.IsDropDownOpen}";
    }

    private void CaptureAutomationState(string label)
    {
        if (string.IsNullOrWhiteSpace(captureDirectory))
        {
            return;
        }

        Directory.CreateDirectory(captureDirectory);
        int sequence = captureSequence++;
        string stem = $"{sequence:000}-{label}";
        SaveScreenshot(Path.Combine(captureDirectory, $"{stem}.png"));

        object state = new
        {
            Filtered = ComboState(FilteredCombo),
            Selection = ComboState(SelectionCombo),
            Bottom = ComboState(BottomCombo)
        };
        File.WriteAllText(
            Path.Combine(captureDirectory, $"{stem}.json"),
            JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static object ComboState(ComboBox comboBox)
    {
        return new
        {
            comboBox.SelectedIndex,
            comboBox.Text,
            comboBox.IsDropDownOpen,
            RealizedItemCount = comboBox.ItemContainerGenerator.RealizedContainers.Count
        };
    }
}
