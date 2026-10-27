using System.Xml.Linq;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Playground;

// Markup of the Playground labs, read as XML: what a lab labels in its UI has to match what its markup declares.
public sealed class PlaygroundLabMarkupTests
{
    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Cerneala.slnx was not found above " + AppContext.BaseDirectory);
    }

    // The ComboBox element that follows the TextBlock with the given Text in its parent panel, and its effective IsEditable:
    // the IsEditable attribute when present, else the default of ComboBox.IsEditableProperty.
    private static (string Name, string? Declared, bool Effective) ComboBoxAfterLabel(string label)
    {
        XDocument document = XDocument.Load(Path.Combine(
            FindRepositoryRoot(), "Playground", "Cerneala.ComboBoxLab", "MainWindow.crn"));
        XElement labelElement = document.Descendants("TextBlock")
            .Single(element => (string?)element.Attribute("Text") == label);
        XElement combo = labelElement.ElementsAfterSelf("ComboBox").First();
        string? declared = (string?)combo.Attribute("IsEditable");
        bool effective = declared is null ? new ComboBox().IsEditable : bool.Parse(declared);
        return ((string?)combo.Attribute("Name") ?? "(unnamed)", declared, effective);
    }

    [Fact]
    public void ComboBoxLabControlTheEditableComboDeclaresItsModeAndIsEditable()
    {
        (string name, string? declared, bool effective) = ComboBoxAfterLabel("Editable + filtering");

        Assert.Equal("FilteredCombo", name);
        Assert.Equal("true", declared);
        Assert.True(effective);
    }

    [Fact]
    public void ComboBoxLabNonEditableTransactionComboIsNotEditable()
    {
        // ComboBox.IsEditableProperty defaults to true, so the combo under "Non-editable transaction" must opt out explicitly.
        (string name, string? declared, bool effective) = ComboBoxAfterLabel("Non-editable transaction");

        Assert.False(
            effective,
            $"The lab labels the ComboBox '{name}' as \"Non-editable transaction\", but its markup declares IsEditable={(declared is null ? "(not set)" : declared)} " +
            $"and ComboBox.IsEditableProperty defaults to {new ComboBox().IsEditable}, so its effective IsEditable is {effective}.");
    }
}
