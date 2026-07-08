using Cerneala.Playground.Samples;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Theming;

namespace Cerneala.Tests.UI.Hosting;

public sealed class AuthoringPreviewContractTests
{
    [Fact]
    public void AuthoringPreviewFirstFrameDoesRetainedWork()
    {
        UiHost host = Host(out _);

        UiFrame frame = host.Update(EmptyFrame(), new UiViewport(360, 240), TimeSpan.Zero);

        Assert.True(frame.Stats.HasWork);
    }

    [Fact]
    public void AuthoringPreviewSecondUnchangedFrameDoesNoRetainedWork()
    {
        UiHost host = Host(out _);
        host.Update(EmptyFrame(), new UiViewport(360, 240), TimeSpan.Zero);

        UiFrame second = host.Update(EmptyFrame(), new UiViewport(360, 240), TimeSpan.Zero);

        Assert.Equal(1, second.Stats.NoWorkFrames);
    }

    [Fact]
    public void AuthoringPreviewCommandDisabledWhenInputIsEmptyAndEnabledWhenInputHasText()
    {
        UiHost host = Host(out AuthoringAppSample sample, out UIRoot root);
        host.Update(EmptyFrame(), new UiViewport(360, 240), TimeSpan.Zero);

        Assert.False(sample.SubmitButton!.IsEnabled);

        sample.NameTextBox!.ReceiveTextInput("Lin");
        root.ProcessFrame();

        Assert.True(sample.SubmitButton.IsEnabled);
    }

    [Fact]
    public void AuthoringPreviewButtonActivationAddsItems()
    {
        UiHost host = Host(out AuthoringAppSample sample, out UIRoot root);
        host.Update(EmptyFrame(), new UiViewport(360, 240), TimeSpan.Zero);
        sample.NameTextBox!.ReceiveTextInput("Lin");
        root.ProcessFrame();

        bool executed = sample.SubmitButton!.ExecuteCommand(new CommandRouter(), root.InputCache.EnsureCurrent(root));

        Assert.True(executed);
        Assert.Contains("Lin", sample.Items);
    }

    [Fact]
    public void AuthoringPreviewSemanticsIncludesTextBoxButtonAndList()
    {
        UiHost host = Host(out _, out UIRoot root);
        host.Update(EmptyFrame(), new UiViewport(360, 240), TimeSpan.Zero);

        SemanticsNode semantics = root.GetSemanticsTree().Root;

        Assert.NotNull(Find(semantics, SemanticsRole.EditableText));
        Assert.NotNull(Find(semantics, SemanticsRole.Button));
        Assert.NotNull(Find(semantics, SemanticsRole.List));
    }

    private static UiHost Host(out AuthoringAppSample sample)
    {
        return Host(out sample, out _);
    }

    private static UiHost Host(out AuthoringAppSample sample, out UIRoot root)
    {
        root = new UIRoot(360, 240);
        root.SetThemeProvider(new ThemeProvider(DefaultTheme.Create()));
        sample = new AuthoringAppSample();
        root.VisualChildren.Add(sample.Build());
        return new UiHost(new UiHostOptions { Root = root });
    }

    private static InputFrame EmptyFrame()
    {
        return new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static SemanticsNode Find(SemanticsNode node, SemanticsRole role)
    {
        if (node.Role == role)
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            try
            {
                return Find(child, role);
            }
            catch (InvalidOperationException)
            {
            }
        }

        throw new InvalidOperationException($"No semantics node with role {role}.");
    }
}
