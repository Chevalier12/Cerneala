using Cerneala.Playground.Samples;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Theming;

namespace Cerneala.Tests.Playground.Samples;

public sealed class AuthoringAppSampleContractTests
{
    [Fact]
    public void AuthoringAppSampleBuildsTextBoxButtonStatusAndList()
    {
        AuthoringAppSample sample = new();

        UIElement root = sample.Build();

        Assert.NotNull(root);
        Assert.NotNull(sample.NameTextBox);
        Assert.NotNull(sample.SubmitButton);
        Assert.NotNull(sample.StatusText);
        Assert.NotNull(sample.ListBox);
    }

    [Fact]
    public void AuthoringAppSampleUsesDefaultAspectChromeForPrimaryButton()
    {
        UIRoot root = ThemedRoot();
        AuthoringAppSample sample = new();
        root.VisualChildren.Add(sample.Build());

        root.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.AspectBase, sample.SubmitButton!.GetValueSource(Control.BackgroundProperty));
    }

    [Fact]
    public void AuthoringAppSampleUsesObservableListAsItemsSource()
    {
        AuthoringAppSample sample = new();
        sample.Build();

        Assert.Same(sample.Items, sample.ListBox!.ItemsSource);
    }

    [Fact]
    public void AuthoringAppSampleUsesTypedBindingForTextEntry()
    {
        AuthoringAppSample sample = new();
        sample.Build();

        sample.NameTextBox!.ReceiveTextInput("Zoe");

        Assert.Equal("Zoe", sample.NameValue.Value);
        Assert.Equal("Ready to add Zoe.", sample.StatusText!.Text);
    }

    private static UIRoot ThemedRoot()
    {
        UIRoot root = new(300, 200);
        root.SetThemeProvider(new ThemeProvider(DefaultTheme.Create()));
        return root;
    }
}
