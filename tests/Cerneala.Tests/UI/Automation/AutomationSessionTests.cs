using Cerneala.UI.Automation;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Automation;

public sealed class AutomationSessionTests
{
    [Fact]
    public void FindsElementsByAutomationIdAndXPath()
    {
        UIElement root = new();
        Button saveButton = new() { Content = "Save" };
        AutomationProperties.SetAutomationId(saveButton, "save-button");
        root.VisualChildren.Add(saveButton);
        RecordingInputDriver input = new();
        AutomationSession session = new(root, input);

        AutomationElement byId = session.FindByAutomationId("save-button");
        AutomationElement byXPath = session.FindByXPath("//Button[@AutomationId='save-button' and @Name='Save']");

        Assert.Same(saveButton, byId.Element);
        Assert.Same(saveButton, byXPath.Element);
    }

    [Fact]
    public void ElementOperationsUseTheConfiguredInputDriver()
    {
        UIElement root = new();
        Button button = new();
        AutomationProperties.SetAutomationId(button, "target");
        root.VisualChildren.Add(button);
        RecordingInputDriver input = new();
        AutomationSession session = new(root, input);

        session.FindByAutomationId("target")
            .Click()
            .PressKey(InputKey.A, AutomationModifiers.Control)
            .SendText("typed");

        Assert.Same(button, input.ClickedElement);
        Assert.Equal((InputKey.A, AutomationModifiers.Control), input.KeyPress);
        Assert.Equal("typed", input.Text);
    }

    [Fact]
    public void RetainedDriverClicksSelectsAllAndTypesThroughInputPipeline()
    {
        UIRoot root = new(240, 80);
        TextBox textBox = new() { Text = "DEFAULT" };
        textBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 40)));
        AutomationProperties.SetAutomationId(textBox, "editor");
        root.VisualChildren.Add(textBox);
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(240, 80)
        });
        AutomationSession session = new(root, new RetainedAutomationInputDriver(host));

        AutomationElement editor = session.FindByXPath("//TextBox[@AutomationId='editor']");
        editor.Click().PressKey(InputKey.A, AutomationModifiers.Control).SendText("X");

        Assert.True(textBox.IsKeyboardFocused);
        Assert.Equal("X", textBox.Text);
    }

    [Fact]
    public void SaveScreenshotUsesTheConfiguredProvider()
    {
        string? capturedPath = null;
        AutomationSession session = new(
            new UIElement(),
            new RecordingInputDriver(),
            path => capturedPath = path);

        session.SaveScreenshot("capture.png");

        Assert.Equal("capture.png", capturedPath);
    }

    [Fact]
    public void ScriptRunnerExecutesSelectorInputAndScreenshotSteps()
    {
        UIElement root = new();
        TextBox editor = new();
        AutomationProperties.SetAutomationId(editor, "editor");
        root.VisualChildren.Add(editor);
        RecordingInputDriver input = new();
        string? capturedPath = null;
        AutomationSession session = new(root, input, path => capturedPath = path);
        const string script = """
            {
              "steps": [
                { "action": "click", "automationId": "editor" },
                { "action": "pressKey", "key": "A", "modifiers": ["Control"] },
                { "action": "sendText", "text": "replacement" },
                { "action": "screenshot", "path": "result.png" }
              ]
            }
            """;

        AutomationScriptRunner.RunJson(session, script, "captures");

        Assert.Same(editor, input.ClickedElement);
        Assert.Equal((InputKey.A, AutomationModifiers.Control), input.KeyPress);
        Assert.Equal("replacement", input.Text);
        Assert.Equal(Path.Combine("captures", "result.png"), capturedPath);
    }

    private sealed class RecordingInputDriver : IAutomationInputDriver
    {
        public UIElement? ClickedElement { get; private set; }

        public (InputKey Key, AutomationModifiers Modifiers)? KeyPress { get; private set; }

        public string? Text { get; private set; }

        public void Click(UIElement target)
        {
            ClickedElement = target;
        }

        public void PressKey(InputKey key, AutomationModifiers modifiers = AutomationModifiers.None)
        {
            KeyPress = (key, modifiers);
        }

        public void SendText(string text)
        {
            Text = text;
        }
    }
}
