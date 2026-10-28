using Cerneala.UI.Controls;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Controls;

public sealed class TextBoxTwoWayBindingTests
{
    [Fact]
    public void TextBoxTwoWayBindingInitializesTextFromSource()
    {
        ObservableValue<string> source = new("hello");
        TextBox textBox = new();

        using IDisposable binding = BindingOperations.BindTwoWay(textBox, TextBox.TextProperty, source);

        Assert.Equal("hello", textBox.Text);
    }

    [Fact]
    public void TextBoxTextInputCommitsToObservableSource()
    {
        UIRoot root = new(200, 80);
        ObservableValue<string> source = new("");
        TextBox textBox = new();
        root.VisualChildren.Add(textBox);
        ElementInputRouteMap routeMap = new ElementInputRouteBuilder().Build(root);
        FocusManager focusManager = new();
        Assert.True(focusManager.Focus(textBox, routeMap));
        using IDisposable binding = BindingOperations.BindTwoWay(textBox, TextBox.TextProperty, source);

        new TextInputBridge().Dispatch([new TextInputSnapshotEvent("a")], focusManager, routeMap);

        Assert.Equal("a", source.Value);
    }

    [Fact]
    public void SourceChangeUpdatesTextBoxWithoutRecursiveLoop()
    {
        ObservableValue<string> source = new("one");
        TextBox textBox = new();
        int sourceChanges = 0;
        source.ValueChanged += (_, _) => sourceChanges++;
        using IDisposable binding = BindingOperations.BindTwoWay(textBox, TextBox.TextProperty, source);

        source.Value = "two";

        Assert.Equal("two", textBox.Text);
        Assert.Equal(1, sourceChanges);
    }

    [Theory]
    [InlineData(false, "Grace")]
    [InlineData(true, "Gra")]
    public void TextBoxTextEndsWithTheSourceValueWhenTheSourceNormalisesTheCommittedValue(bool normalise, string expected)
    {
        ObservableValue<string> source = new("Ada");
        if (normalise)
        {
            source.ValueChanged += (_, args) =>
            {
                if (args.NewValue.Length > 3)
                {
                    source.Value = args.NewValue[..3];
                }
            };
        }

        TextBox textBox = new();
        using IDisposable binding = BindingOperations.BindTwoWay(textBox, TextBox.TextProperty, source);

        textBox.Text = "Grace";

        Assert.Equal(expected, source.Value);
        Assert.Equal(source.Value, textBox.Text);
    }

    [Fact]
    public void TextBoxCommitThatTheSourceKeepsIsNotWrittenBackToTheTextBox()
    {
        ObservableValue<string> source = new("Ada");
        TextBox textBox = new();
        using IDisposable binding = BindingOperations.BindTwoWay(textBox, TextBox.TextProperty, source);
        int textChanges = 0;
        textBox.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, TextBox.TextProperty))
            {
                textChanges++;
            }
        };

        textBox.Text = "Grace";

        Assert.Equal("Grace", source.Value);
        Assert.Equal("Grace", textBox.Text);
        Assert.Equal(1, textChanges);
    }

    [Fact]
    public void DisposedTextBoxBindingStopsBothDirections()
    {
        ObservableValue<string> source = new("one");
        TextBox textBox = new();
        IDisposable binding = BindingOperations.BindTwoWay(textBox, TextBox.TextProperty, source);

        binding.Dispose();
        source.Value = "two";
        textBox.ReceiveTextInput("!");

        Assert.Equal("one!", textBox.Text);
        Assert.Equal("two", source.Value);
    }
}
