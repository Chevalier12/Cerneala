using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void BindingStageFour_GeneratedSourceUsesTypedFactoriesForEveryBindingShape()
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;
            public sealed class ViewModel : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public bool Enabled { get; set; } = true;
                public string Name { get; set; } = "Ana";
                public ChildViewModel Child { get; set; } = new();
            }
            public sealed class ChildViewModel : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Name { get; set; } = "Copil";
            }
            """;
        const string markup = """
            <StackPanel DataType="TestInput.ViewModel">
              <TextBlock Text="$DataContext.Child.Name" />
              <TextBlock Text="Salut $DataContext.Name / $DataContext.Name" />
              <ProgressBar Value="$Later.Value" />
              <Slider Name="Later" Value="20" />
              <TextBlock IsVisible="True" IsEnabled="$self.IsVisible" />
              <TextBlock Text="Base">
                @when $DataContext.Enabled { Text = $DataContext.Name; }
              </TextBlock>
              <Button Content="Owner">
                @template {
                  <StackPanel>
                    <ContentPresenter Content="$owner.Content:OneWay" />
                    <TextBlock Text="$DataContext.Name" />
                  </StackPanel>
                }
              </Button>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "TypedBindingEmission.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);
        string generated = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("AttachPropertyBinding<", generated);
        Assert.Contains("AttachInterpolatedStringBinding", generated);
        Assert.Contains("CreateConditionalPropertyBinding<", generated);
        Assert.Contains(
            "ObserveProperty(Later, global::Cerneala.UI.Controls.Primitives.RangeBase.ValueProperty)",
            generated);
        Assert.Contains("new global::Cerneala.UI.Markup.MarkupDataPathSegment(\"Child\"", generated);
        Assert.Contains("global::TestInput.ChildViewModel", generated);
        Assert.Contains(".RegisterLifetime(", generated);
        Assert.DoesNotContain("GetProperty(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Reflection", generated, StringComparison.Ordinal);
        Assert.True(compilation.GetDiagnostics().All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error));
    }

    [Fact]
    public void BindingStageFour_ConditionalProvidersSubscribeOnlyWhileWinningAndDetachCleanly()
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;
            public sealed class ConditionalViewModel : INotifyPropertyChanged
            {
                private PropertyChangedEventHandler? changed;
                private bool useShort = true;
                private bool useLong;
                public event PropertyChangedEventHandler? PropertyChanged
                {
                    add { changed += value; SubscriberCount++; }
                    remove { changed -= value; SubscriberCount--; }
                }
                public int SubscriberCount { get; private set; }
                public bool UseShort { get => useShort; set { useShort = value; Raise(nameof(UseShort)); } }
                public bool UseLong { get => useLong; set { useLong = value; Raise(nameof(UseLong)); } }
                public string ShortName { get; set; } = "Scurt";
                public string LongName { get; set; } = "Lung";
                private void Raise(string name) => changed?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            """;
        const string markup = """
            <StackPanel DataType="TestInput.ConditionalViewModel">
              <TextBox Text="Base">
                @when $DataContext.UseShort { Text = $DataContext.ShortName:TwoWay; }
                @when $DataContext.UseLong { Text = $DataContext.LongName:TwoWay; }
              </TextBox>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "ConditionalProviderLifecycle.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly assembly = EmitBindingStageFourAssembly(compilation);
        Type viewModelType = assembly.GetType("TestInput.ConditionalViewModel", throwOnError: true)!;
        object viewModel = Activator.CreateInstance(viewModelType)!;
        StackPanel panel = Assert.IsType<StackPanel>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi.ConditionalProviderLifecycleFactory",
            viewModel));
        TextBox editor = Assert.IsType<TextBox>(panel.VisualChildren[0]);

        Assert.Equal(3, viewModelType.GetProperty("SubscriberCount")!.GetValue(viewModel));
        viewModelType.GetProperty("UseShort")!.SetValue(viewModel, false);
        Assert.Equal(2, viewModelType.GetProperty("SubscriberCount")!.GetValue(viewModel));
        Assert.Equal("Base", editor.Text);
        viewModelType.GetProperty("UseLong")!.SetValue(viewModel, true);
        Assert.Equal(3, viewModelType.GetProperty("SubscriberCount")!.GetValue(viewModel));
        Assert.Equal("Lung", editor.Text);

        UIRoot root = new();
        ElementLifecycle.AttachSubtree(root, panel);
        ElementLifecycle.DetachSubtree(root, panel);
        Assert.Equal(0, viewModelType.GetProperty("SubscriberCount")!.GetValue(viewModel));
        viewModelType.GetProperty("UseLong")!.SetValue(viewModel, false);
        Assert.Equal("Base", editor.Text);
        ElementLifecycle.AttachSubtree(root, panel);
        Assert.Equal(2, viewModelType.GetProperty("SubscriberCount")!.GetValue(viewModel));
        Assert.Equal("Base", editor.Text);
    }

    [Fact]
    public void BindingStageFour_TemplateLifetimeDisposesGeneratedBindingOnTemplateSwap()
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;
            public sealed class TemplateViewModel : INotifyPropertyChanged
            {
                private PropertyChangedEventHandler? changed;
                public event PropertyChangedEventHandler? PropertyChanged
                {
                    add { changed += value; SubscriberCount++; }
                    remove { changed -= value; SubscriberCount--; }
                }
                public int SubscriberCount { get; private set; }
                public string Name { get; set; } = "Initial";
            }
            """;
        const string markup = """
            <Button DataType="TestInput.TemplateViewModel">
              @template { <TextBlock Text="$DataContext.Name" /> }
            </Button>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "TemplateBindingLifetime.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly assembly = EmitBindingStageFourAssembly(compilation);
        Type viewModelType = assembly.GetType("TestInput.TemplateViewModel", throwOnError: true)!;
        object viewModel = Activator.CreateInstance(viewModelType)!;
        Button button = Assert.IsType<Button>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi.TemplateBindingLifetimeFactory",
            viewModel));

        UIRoot root = new();
        ElementLifecycle.AttachSubtree(root, button);
        Assert.Equal(1, viewModelType.GetProperty("SubscriberCount")!.GetValue(viewModel));
        button.ComponentTemplate = new ComponentTemplate<Button>("replacement", _ => new Border());
        Assert.Equal(0, viewModelType.GetProperty("SubscriberCount")!.GetValue(viewModel));
    }

    [Fact]
    public void BindingStageFour_GenericWindowAndUserControlRunBindingsEndToEnd()
    {
        const string userControlSource = """
            using System.ComponentModel;
            namespace TestInput.Views
            {
                public sealed class UserControlViewModel : INotifyPropertyChanged
                {
                    private string name = "Initial";
                    public event PropertyChangedEventHandler? PropertyChanged;
                    public string Name { get => name; set { name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); } }
                }
                public partial class BindingView : Cerneala.UI.Controls.UserControl<UserControlViewModel> { }
            }
            """;
        GeneratorRunResult userControlResult = RunPairedGenerator(
            "Views/BindingView.cui.xml",
            "<UserControl><TextBlock Text=\"$DataContext.Name\" /></UserControl>",
            userControlSource,
            out Compilation userControlCompilation);
        Assert.DoesNotContain(userControlResult.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly userControlAssembly = EmitBindingStageFourAssembly(userControlCompilation);
        Type userControlViewModelType = userControlAssembly.GetType("TestInput.Views.UserControlViewModel", throwOnError: true)!;
        Type userControlType = userControlAssembly.GetType("TestInput.Views.BindingView", throwOnError: true)!;
        object userControlViewModel = Activator.CreateInstance(userControlViewModelType)!;
        UserControl userControl = Assert.IsAssignableFrom<UserControl>(Activator.CreateInstance(userControlType, userControlViewModel));
        UIRoot userControlRoot = new();
        ElementLifecycle.AttachSubtree(userControlRoot, userControl);
        TextBlock userControlText = Assert.IsType<TextBlock>(userControl.ComponentTemplateInstance!.Root);
        Assert.Equal("Initial", userControlText.Text);
        userControlViewModelType.GetProperty("Name")!.SetValue(userControlViewModel, "Changed");
        Assert.Equal("Changed", userControlText.Text);

        const string windowSource = """
            using System.ComponentModel;
            namespace TestInput.Views
            {
                public sealed class WindowViewModel : INotifyPropertyChanged
                {
                    private string name = "Initial";
                    public event PropertyChangedEventHandler? PropertyChanged;
                    public string Name { get => name; set { name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); } }
                }
                public partial class BindingWindow : Cerneala.UI.Controls.Window<WindowViewModel> { }
            }
            """;
        GeneratorRunResult windowResult = RunPairedGenerator(
            "Views/BindingWindow.cui.xml",
            "<Window><TextBlock Text=\"$DataContext.Name\" /></Window>",
            windowSource,
            out Compilation windowCompilation);
        Assert.DoesNotContain(windowResult.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly windowAssembly = EmitBindingStageFourAssembly(windowCompilation);
        Type windowViewModelType = windowAssembly.GetType("TestInput.Views.WindowViewModel", throwOnError: true)!;
        Type windowType = windowAssembly.GetType("TestInput.Views.BindingWindow", throwOnError: true)!;
        object windowViewModel = Activator.CreateInstance(windowViewModelType)!;
        Window window = Assert.IsAssignableFrom<Window>(Activator.CreateInstance(windowType, windowViewModel));
        UIRoot windowRoot = new();
        ElementLifecycle.AttachSubtree(windowRoot, window);
        TextBlock windowText = Assert.IsType<TextBlock>(window.Content);
        Assert.Equal("Initial", windowText.Text);
        windowViewModelType.GetProperty("Name")!.SetValue(windowViewModel, "Changed");
        Assert.Equal("Changed", windowText.Text);
    }

    private static Assembly EmitBindingStageFourAssembly(Compilation compilation)
    {
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        return Assembly.Load(stream.ToArray());
    }
}
