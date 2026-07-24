using System.Reflection;
using Cerneala.UI.Controls;
using Cerneala.UI.Input;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls;

public sealed class TextBoxArchitectureTests
{
    [Fact]
    public void TextPropertyIsOwnedByConcreteTextBox()
    {
        Assert.Equal(typeof(TextBox), TextBox.TextProperty.OwnerType);
        Assert.Equal(typeof(TextBox), TextBox.TextChangedEvent.OwnerType);
        Assert.Equal(typeof(TextBox), TextBox.SelectionChangedEvent.OwnerType);
    }

    [Fact]
    public void EditingEngineTypesAreInternalAndTextBoxBaseIsRemoved()
    {
        Assembly assembly = typeof(TextBox).Assembly;

        Assert.Null(assembly.GetType("Cerneala.UI.Controls.TextBoxBase"));
        Assert.False(assembly.GetType("Cerneala.UI.Text.TextEditor")!.IsPublic);
        Assert.False(assembly.GetType("Cerneala.UI.Text.TextDocument")!.IsPublic);
        Assert.False(assembly.GetType("Cerneala.UI.Text.TextEditingController")!.IsPublic);
        Assert.False(assembly.GetType("Cerneala.UI.Text.TextCompositionManager")!.IsPublic);
        Assert.False(assembly.GetType("Cerneala.UI.Text.TextEditorSnapshot")!.IsPublic);
        Assert.False(assembly.GetType("Cerneala.UI.Text.UndoRedoStack")!.IsPublic);
    }

    [Fact]
    public void TextInputsExposeCaretBrushInsteadOfCaretColor()
    {
        Assert.Equal(typeof(Brush), typeof(TextBox).GetProperty("CaretBrush")!.PropertyType);
        Assert.Equal(typeof(Brush), typeof(PasswordBox).GetProperty("CaretBrush")!.PropertyType);
        Assert.Null(typeof(TextBox).GetProperty("CaretColor"));
        Assert.Null(typeof(PasswordBox).GetProperty("CaretColor"));
        Assert.Equal(typeof(TextBox), TextBox.CaretBrushProperty.OwnerType);
        Assert.Equal(typeof(PasswordBox), PasswordBox.CaretBrushProperty.OwnerType);
    }

    [Fact]
    public void DerivedTextBoxCanNormalizeInputAndObserveChanges()
    {
        UpperTextBox textBox = new();

        textBox.ReceiveTextInput("ink");

        Assert.Equal("INK", textBox.Text);
        Assert.Equal(1, textBox.TextChangeCount);
    }

    [Fact]
    public void DerivedPasswordBoxCanNormalizeInputAndObserveChanges()
    {
        NumericPasswordBox passwordBox = new();

        passwordBox.DispatchTextInput("a1b2");

        Assert.Equal("12", passwordBox.Password);
        Assert.Equal(1, passwordBox.PasswordChangeCount);
    }

    private sealed class UpperTextBox : TextBox
    {
        public int TextChangeCount { get; private set; }

        protected override string NormalizeTextInput(string text) =>
            base.NormalizeTextInput(text).ToUpperInvariant();

        protected override void OnTextChanged(TextChangedEventArgs args)
        {
            TextChangeCount++;
            base.OnTextChanged(args);
        }
    }

    private sealed class NumericPasswordBox : PasswordBox
    {
        public int PasswordChangeCount { get; private set; }

        public void DispatchTextInput(string text)
        {
            RoutedEventArgs args = new TextCompositionEventArgs(
                InputEvents.TextInputEvent,
                this,
                text);
            RaiseEvent(args);
        }

        protected override string NormalizeTextInput(string text) =>
            new(base.NormalizeTextInput(text).Where(char.IsDigit).ToArray());

        protected override void OnPasswordChanged(RoutedEventArgs args)
        {
            PasswordChangeCount++;
            base.OnPasswordChanged(args);
        }
    }
}
