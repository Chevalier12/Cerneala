using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void MarkupEventHandlersReceiveTheElementAsTypedSender()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            using Cerneala.UI.Controls.Primitives;
            using Cerneala.UI.Elements;
            using Cerneala.UI.Input;
            namespace TestInput.Views;
            public partial class SenderView : UserControl
            {
                public object? SaveSender { get; private set; }
                public object? OpenSender { get; private set; }
                public object? CloseSender { get; private set; }
                public object? VolumeSender { get; private set; }
                public object? RootSender { get; private set; }
                private void OnSave(Button sender, RoutedEventArgs args) => SaveSender = sender;
                private void OnOpen(UIElement sender, RoutedEventArgs args) => OpenSender = sender;
                private void OnClose(object sender, RoutedEventArgs args) => CloseSender = sender;
                private void OnVolume(Slider sender, RoutedPropertyChangedEventArgs<float> args) => VolumeSender = sender;
                private void OnRootMouseDown(SenderView sender, RoutedEventArgs args) => RootSender = sender;
            }
            """;
        const string markup = """
            <UserControl MouseDown="OnRootMouseDown">
              <StackPanel>
                <Button Name="SaveButton" Content="Save" Click="OnSave" />
                <Button Name="OpenButton" Content="Open" Click="OnOpen" />
                <Button Name="CloseButton" Content="Close" Click="OnClose" />
                <Slider Name="Volume" Maximum="10" ValueChanged="OnVolume" />
              </StackPanel>
            </UserControl>
            """;

        GeneratorRunResult result = RunPairedGenerator("Views/SenderView.crn", markup, inputSource, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.Empty(result.Diagnostics);
        Assert.Contains(
            "SaveButton.Click += (__cernealaSender, __cernealaArgs) => this.OnSave(SaveButton, __cernealaArgs);",
            generatedSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "this.MouseDown += (__cernealaSender, __cernealaArgs) => this.OnRootMouseDown(this, __cernealaArgs);",
            generatedSource,
            StringComparison.Ordinal);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        Type viewType = assembly.GetType("TestInput.Views.SenderView", throwOnError: true)!;
        UserControl view = Assert.IsAssignableFrom<UserControl>(Activator.CreateInstance(viewType));
        Button save = GetNamedElement<Button>(viewType, view, "SaveButton");
        Button open = GetNamedElement<Button>(viewType, view, "OpenButton");
        Button close = GetNamedElement<Button>(viewType, view, "CloseButton");
        Slider volume = GetNamedElement<Slider>(viewType, view, "Volume");

        save.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, save));
        open.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, open));
        close.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, close));
        volume.Value = 5;
        view.RaiseEvent(new MouseButtonEventArgs(InputEvents.MouseDownEvent, view, InputMouseButton.Left, 0, 0, 1));

        Assert.Same(save, viewType.GetProperty("SaveSender")!.GetValue(view));
        Assert.Same(open, viewType.GetProperty("OpenSender")!.GetValue(view));
        Assert.Same(close, viewType.GetProperty("CloseSender")!.GetValue(view));
        Assert.Same(volume, viewType.GetProperty("VolumeSender")!.GetValue(view));
        Assert.Same(view, viewType.GetProperty("RootSender")!.GetValue(view));
    }

    [Fact]
    public void SceneSpriteMouseDownHandlerReceivesTheSprite()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            using Cerneala.UI.Input;
            namespace TestInput.Views;
            public partial class LidView : UserControl
            {
                public Sprite2D? Opened { get; private set; }
                private void OnOpen(Sprite2D sender, RoutedEventArgs args) => Opened = sender;
            }
            """;

        GeneratorRunResult result = RunPairedGenerator("Views/LidView.crn", SpriteLidMarkup, inputSource, out Compilation compilation);

        Assert.Empty(result.Diagnostics);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        Type viewType = assembly.GetType("TestInput.Views.LidView", throwOnError: true)!;
        UserControl view = Assert.IsAssignableFrom<UserControl>(Activator.CreateInstance(viewType));
        Sprite2D lid = GetNamedElement<Sprite2D>(viewType, view, "Lid");

        lid.RaiseEvent(new MouseButtonEventArgs(InputEvents.MouseDownEvent, lid, InputMouseButton.Left, 0, 0, 1));

        Assert.Same(lid, viewType.GetProperty("Opened")!.GetValue(view));
    }

    [Fact]
    public void MarkupEventHandlerWhoseSenderTypeDoesNotAcceptTheElementIsRejected()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            using Cerneala.UI.Input;
            namespace TestInput.Views;
            public partial class LidView : UserControl
            {
                private void OnOpen(Button sender, RoutedEventArgs args) { }
            }
            """;

        GeneratorRunResult result = RunPairedGenerator("Views/LidView.crn", SpriteLidMarkup, inputSource, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI009", "Views/LidView.crn");
        Assert.Contains("OnOpen", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    private const string SpriteLidMarkup = """
        <UserControl>
          <RenderSurface2D>
            <RenderSurface2D.Scene>
              <Scene2D>
                <Sprite2D Name="Lid" Width="16" Height="16" MouseDown="OnOpen" />
              </Scene2D>
            </RenderSurface2D.Scene>
          </RenderSurface2D>
        </UserControl>
        """;

    private static T GetNamedElement<T>(Type viewType, object view, string name)
        where T : UIElement
    {
        object? value = viewType.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(view)
            ?? viewType.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(view);
        return Assert.IsType<T>(value);
    }
}
