using System;
using System.Globalization;
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
    public void MarkupBindingStageZero_OneWaySimpleNestedAndStringProjectionAreReactive()
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;

            public sealed class BindingRoot : INotifyPropertyChanged
            {
                private string name = "Ana";
                private int count = 1234;
                private int? optionalCount;
                private BindingChild? type = new() { Name = "Primul" };
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Name { get => name; set { name = value; Changed(nameof(Name)); } }
                public int Count { get => count; set { count = value; Changed(nameof(Count)); } }
                public int? OptionalCount { get => optionalCount; set { optionalCount = value; Changed(nameof(OptionalCount)); } }
                public BindingChild? Type { get => type; set { type = value; Changed(nameof(Type)); } }
                private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

            public sealed class BindingChild : INotifyPropertyChanged
            {
                private string? name;
                public event PropertyChangedEventHandler? PropertyChanged;
                public string? Name { get => name; set { name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); } }
            }
            """;
        const string markup = """
            <StackPanel DataType="TestInput.BindingRoot">
              <TextBlock Text="$DataContext.Name:OneWay" />
              <TextBlock Text="$DataContext.Count" />
              <TextBlock Text="$DataContext.OptionalCount" />
              <TextBlock Text="$DataContext.Type.Name:OneWay" />
            </StackPanel>
            """;

        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ro-RO");
            GeneratorRunResult result = RunGeneratorWithInput(
                "BindingOneWay.cui.xml",
                markup,
                inputSource,
                out Compilation compilation);

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assembly assembly = EmitBindingTestAssembly(compilation);
            Type rootType = assembly.GetType("TestInput.BindingRoot", throwOnError: true)!;
            Type childType = assembly.GetType("TestInput.BindingChild", throwOnError: true)!;
            object viewModel = Activator.CreateInstance(rootType)!;
            object firstChild = rootType.GetProperty("Type")!.GetValue(viewModel)!;
            StackPanel panel = Assert.IsType<StackPanel>(InvokeBindingTestCreate(
                assembly,
                "Cerneala.GeneratedUi.BindingOneWayFactory",
                viewModel));
            TextBlock[] values = panel.VisualChildren.Cast<TextBlock>().ToArray();

            Assert.Equal("Ana", values[0].Text);
            Assert.Equal(Convert.ToString(1234, CultureInfo.CurrentCulture), values[1].Text);
            Assert.Equal(string.Empty, values[2].Text);
            Assert.Equal("Primul", values[3].Text);

            rootType.GetProperty("Name")!.SetValue(viewModel, "Ioana");
            rootType.GetProperty("Count")!.SetValue(viewModel, 42);
            rootType.GetProperty("OptionalCount")!.SetValue(viewModel, 7);
            childType.GetProperty("Name")!.SetValue(firstChild, "Vechi");
            Assert.Equal(new[] { "Ioana", "42", "7", "Vechi" }, values.Select(value => value.Text));

            object secondChild = Activator.CreateInstance(childType)!;
            childType.GetProperty("Name")!.SetValue(secondChild, "Nou");
            rootType.GetProperty("Type")!.SetValue(viewModel, secondChild);
            Assert.Equal("Nou", values[3].Text);
            childType.GetProperty("Name")!.SetValue(firstChild, "Ignorat");
            Assert.Equal("Nou", values[3].Text);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void MarkupBindingStageZero_InterpolationsObserveFragmentsAndConsumeEscapes()
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;

            public sealed class InterpolationRoot : INotifyPropertyChanged
            {
                private string name = "Ana";
                private int count = 3;
                private InterpolationChild? type = new() { Name = "Primul" };
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Name { get => name; set { name = value; Changed(nameof(Name)); } }
                public int Count { get => count; set { count = value; Changed(nameof(Count)); } }
                public InterpolationChild? Type { get => type; set { type = value; Changed(nameof(Type)); } }
                private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

            public sealed class InterpolationChild : INotifyPropertyChanged
            {
                private string? name;
                public event PropertyChangedEventHandler? PropertyChanged;
                public string? Name { get => name; set { name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); } }
            }
            """;
        const string markup = """
            <StackPanel DataType="TestInput.InterpolationRoot">
              <TextBlock Text="Salut, $DataContext.Name" />
              <TextBlock Text="Comenzi: $DataContext.Count, utilizator: $DataContext.Name/$DataContext.Name, tip: $DataContext.Type.Name" />
              <TextBlock Text="Literal: \$DataContext.Name:OneWay" />
              <TextBlock Text="Initial" IsEnabled="True">
                @when IsEnabled { Text = "Conditional: \$DataContext.Name:TwoWay"; }
              </TextBlock>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "BindingInterpolation.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly assembly = EmitBindingTestAssembly(compilation);
        Type rootType = assembly.GetType("TestInput.InterpolationRoot", throwOnError: true)!;
        Type childType = assembly.GetType("TestInput.InterpolationChild", throwOnError: true)!;
        object viewModel = Activator.CreateInstance(rootType)!;
        object firstChild = rootType.GetProperty("Type")!.GetValue(viewModel)!;
        StackPanel panel = Assert.IsType<StackPanel>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi.BindingInterpolationFactory",
            viewModel));
        TextBlock[] values = panel.VisualChildren.Cast<TextBlock>().ToArray();

        Assert.Equal("Salut, Ana", values[0].Text);
        Assert.Equal("Comenzi: 3, utilizator: Ana/Ana, tip: Primul", values[1].Text);
        Assert.Equal("Literal: $DataContext.Name:OneWay", values[2].Text);
        Assert.Equal("Conditional: $DataContext.Name:TwoWay", values[3].Text);

        rootType.GetProperty("Name")!.SetValue(viewModel, "Mara");
        rootType.GetProperty("Count")!.SetValue(viewModel, 4);
        childType.GetProperty("Name")!.SetValue(firstChild, null);
        Assert.Equal("Salut, Mara", values[0].Text);
        Assert.Equal("Comenzi: 4, utilizator: Mara/Mara, tip: ", values[1].Text);
        Assert.Equal("Literal: $DataContext.Name:OneWay", values[2].Text);
        Assert.Equal("Conditional: $DataContext.Name:TwoWay", values[3].Text);

        object secondChild = Activator.CreateInstance(childType)!;
        childType.GetProperty("Name")!.SetValue(secondChild, "Nou");
        rootType.GetProperty("Type")!.SetValue(viewModel, secondChild);
        Assert.Equal("Comenzi: 4, utilizator: Mara/Mara, tip: Nou", values[1].Text);
        childType.GetProperty("Name")!.SetValue(firstChild, "Ignorat");
        Assert.Equal("Comenzi: 4, utilizator: Mara/Mara, tip: Nou", values[1].Text);
    }

    [Fact]
    public void MarkupBindingStageZero_InterpolationsSupportNamedSelfOwnerAndTemplatePartSources()
    {
        const string markup = """
            <StackPanel>
              <Slider Name="VolumeSlider" Maximum="100" Value="40" />
              <TextBlock Text="Volum: $VolumeSlider.Value" />
              <TextBlock IsEnabled="True" Text="Self: $self.IsEnabled" />
              <Button Name="Host" Content="Go">
                @template
                {
                  <Border Name="Chrome" IsEnabled="True">
                    <TextBlock Text="Owner: $owner.Content" />
                  </Border>
                }
              </Button>
              <TextBlock Text="Part: $Host.parts.$Chrome.IsEnabled" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator(
            "BindingInterpolationScopes.cui.xml",
            markup,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly assembly = EmitBindingTestAssembly(compilation);
        StackPanel panel = Assert.IsType<StackPanel>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi.BindingInterpolationScopesFactory"));
        Slider slider = Assert.IsType<Slider>(panel.VisualChildren[0]);
        TextBlock namedText = Assert.IsType<TextBlock>(panel.VisualChildren[1]);
        TextBlock selfText = Assert.IsType<TextBlock>(panel.VisualChildren[2]);
        Button host = Assert.IsType<Button>(panel.VisualChildren[3]);
        TextBlock partText = Assert.IsType<TextBlock>(panel.VisualChildren[4]);
        Border chrome = Assert.IsType<Border>(host.ComponentTemplateInstance!.Parts["Chrome"]);
        TextBlock ownerText = Assert.IsType<TextBlock>(chrome.Child);

        Assert.Equal("Volum: 40", namedText.Text);
        Assert.Equal("Self: True", selfText.Text);
        Assert.Equal("Owner: Go", ownerText.Text);
        Assert.Equal("Part: True", partText.Text);

        slider.Value = 55;
        selfText.IsEnabled = false;
        host.Content = "Changed";
        chrome.IsEnabled = false;
        Assert.Equal("Volum: 55", namedText.Text);
        Assert.Equal("Self: False", selfText.Text);
        Assert.Equal("Owner: Changed", ownerText.Text);
        Assert.Equal("Part: False", partText.Text);
    }

    [Fact]
    public void MarkupBindingStageZero_UnobservableClrOwnerReportsActionableDiagnostic()
    {
        const string inputSource = """
            namespace TestInput;
            public sealed class PlainViewModel
            {
                public string Name { get; set; } = "Static";
            }
            """;
        const string markup = """
            <TextBlock DataType="TestInput.PlainViewModel" Text="$DataContext.Name" />
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "UnobservableBinding.cui.xml",
            markup,
            inputSource,
            out _);

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Severity == DiagnosticSeverity.Error &&
                candidate.GetMessage().Contains("INotifyPropertyChanged", StringComparison.Ordinal));
        Assert.Equal("UnobservableBinding.cui.xml", diagnostic.Location.GetLineSpan().Path);
    }

    [Fact]
    public void MarkupBindingStageZero_OffThreadNotificationFailsBeforeReadingOrWriting()
    {
        const string inputSource = """
            using System;
            using System.ComponentModel;
            using System.Threading;
            namespace TestInput;

            public sealed class ThreadedViewModel : INotifyPropertyChanged
            {
                private string name = "Initial";
                private int readCount;
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Name { get { readCount++; return name; } }
                public int ReadCount => readCount;
                public int WorkerThreadId { get; private set; }
                public void ResetReadCount() => readCount = 0;
                public Exception? RaiseNameChangedFromWorker()
                {
                    Exception? failure = null;
                    using ManualResetEventSlim completed = new(false);
                    Thread worker = new(() =>
                    {
                        WorkerThreadId = Environment.CurrentManagedThreadId;
                        try
                        {
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                        }
                        catch (Exception error)
                        {
                            failure = error;
                        }
                        finally
                        {
                            completed.Set();
                        }
                    });
                    worker.Start();
                    completed.Wait();
                    worker.Join();
                    return failure;
                }
            }
            """;
        const string markup = """
            <TextBlock DataType="TestInput.ThreadedViewModel" Text="$DataContext.Name" />
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "OffThreadBinding.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly assembly = EmitBindingTestAssembly(compilation);
        Type viewModelType = assembly.GetType("TestInput.ThreadedViewModel", throwOnError: true)!;
        object viewModel = Activator.CreateInstance(viewModelType)!;
        int uiThreadId = Environment.CurrentManagedThreadId;
        TextBlock text = Assert.IsType<TextBlock>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi.OffThreadBindingFactory",
            viewModel));
        viewModelType.GetMethod("ResetReadCount")!.Invoke(viewModel, null);

        Exception failure = Assert.IsType<InvalidOperationException>(
            viewModelType.GetMethod("RaiseNameChangedFromWorker")!.Invoke(viewModel, null));
        int workerThreadId = (int)viewModelType.GetProperty("WorkerThreadId")!.GetValue(viewModel)!;
        Assert.Contains("$DataContext.Name", failure.Message, StringComparison.Ordinal);
        Assert.Contains(uiThreadId.ToString(CultureInfo.InvariantCulture), failure.Message, StringComparison.Ordinal);
        Assert.Contains(workerThreadId.ToString(CultureInfo.InvariantCulture), failure.Message, StringComparison.Ordinal);
        Assert.Equal(0, viewModelType.GetProperty("ReadCount")!.GetValue(viewModel));
        Assert.Equal("Initial", text.Text);
    }

    [Fact]
    public void MarkupBindingStageZero_InheritedDataContextReplacementRebindsAndUnsubscribes()
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;
            public sealed class InheritedViewModel : INotifyPropertyChanged
            {
                private string name;
                public InheritedViewModel(string name) => this.name = name;
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Name { get => name; set { name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); } }
            }
            """;
        const string markup = """
            <StackPanel DataType="TestInput.InheritedViewModel">
              <TextBlock Text="$DataContext.Name" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "InheritedBinding.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly assembly = EmitBindingTestAssembly(compilation);
        Type viewModelType = assembly.GetType("TestInput.InheritedViewModel", throwOnError: true)!;
        object first = Activator.CreateInstance(viewModelType, "Primul")!;
        object second = Activator.CreateInstance(viewModelType, "Al doilea")!;
        StackPanel panel = Assert.IsType<StackPanel>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi.InheritedBindingFactory",
            first));
        TextBlock child = Assert.IsType<TextBlock>(panel.VisualChildren[0]);
        Assert.Equal("Primul", child.Text);

        panel.DataContext = second;
        Assert.Equal("Al doilea", child.Text);
        viewModelType.GetProperty("Name")!.SetValue(first, "Ignorat");
        Assert.Equal("Al doilea", child.Text);
        viewModelType.GetProperty("Name")!.SetValue(second, "Curent");
        Assert.Equal("Curent", child.Text);
    }

    [Fact]
    public void MarkupBindingStageZero_TwoWayAndNamedEndpointsSupportBackwardAndForwardReferences()
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;
            public sealed class EditorViewModel : INotifyPropertyChanged
            {
                private string name = "Initial";
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Name { get => name; set { name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); } }
            }
            """;
        const string markup = """
            <StackPanel DataType="TestInput.EditorViewModel">
              <TextBox Text="$DataContext.Name:TwoWay" />
              <Slider Name="Before" Maximum="100" Value="40" />
              <ProgressBar Maximum="100" Value="$Before.Value" />
              <ProgressBar Maximum="100" Value="$Before.Value:OneWay" />
              <ProgressBar Maximum="100" Value="$Before.Value:TwoWay" />
              <ProgressBar Maximum="100" Value="$After.Value" />
              <Slider Name="After" Maximum="100" Value="25" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "TwoWayNamedBinding.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly assembly = EmitBindingTestAssembly(compilation);
        Type viewModelType = assembly.GetType("TestInput.EditorViewModel", throwOnError: true)!;
        object viewModel = Activator.CreateInstance(viewModelType)!;
        StackPanel panel = Assert.IsType<StackPanel>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi.TwoWayNamedBindingFactory",
            viewModel));
        TextBox editor = Assert.IsType<TextBox>(panel.VisualChildren[0]);
        Slider before = Assert.IsType<Slider>(panel.VisualChildren[1]);
        ProgressBar implicitOneWay = Assert.IsType<ProgressBar>(panel.VisualChildren[2]);
        ProgressBar explicitOneWay = Assert.IsType<ProgressBar>(panel.VisualChildren[3]);
        ProgressBar twoWay = Assert.IsType<ProgressBar>(panel.VisualChildren[4]);
        ProgressBar forward = Assert.IsType<ProgressBar>(panel.VisualChildren[5]);
        Slider after = Assert.IsType<Slider>(panel.VisualChildren[6]);

        Assert.Equal("Initial", editor.Text);
        editor.Text = "Local";
        Assert.Equal("Local", viewModelType.GetProperty("Name")!.GetValue(viewModel));
        viewModelType.GetProperty("Name")!.SetValue(viewModel, "Source");
        Assert.Equal("Source", editor.Text);

        Assert.Equal(40, implicitOneWay.Value);
        Assert.Equal(40, explicitOneWay.Value);
        Assert.Equal(40, twoWay.Value);
        Assert.Equal(25, forward.Value);
        before.Value = 55;
        Assert.Equal(55, implicitOneWay.Value);
        Assert.Equal(55, explicitOneWay.Value);
        Assert.Equal(55, twoWay.Value);
        twoWay.Value = 61;
        Assert.Equal(61, before.Value);
        Assert.Equal(61, implicitOneWay.Value);
        after.Value = 33;
        Assert.Equal(33, forward.Value);
    }

    [Fact]
    public void MarkupBindingStageZero_SelfBindingAllowsAnotherPropertyAndRejectsIdentity()
    {
        const string validMarkup = """
            <TextBlock IsVisible="True" IsEnabled="$self.IsVisible:OneWay" />
            """;
        GeneratorRunResult valid = RunGenerator(
            "SelfBinding.cui.xml",
            validMarkup,
            out Compilation compilation);
        Assert.DoesNotContain(valid.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly assembly = EmitBindingTestAssembly(compilation);
        TextBlock text = Assert.IsType<TextBlock>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi.SelfBindingFactory"));
        Assert.True(text.IsEnabled);
        text.IsVisible = false;
        Assert.False(text.IsEnabled);

        GeneratorRunResult invalid = RunGenerator(
            "SelfBindingLoop.cui.xml",
            "<TextBlock IsEnabled=\"$self.IsEnabled\" />",
            out _);
        Diagnostic diagnostic = Assert.Single(
            invalid.Diagnostics,
            candidate => candidate.Severity == DiagnosticSeverity.Error &&
                candidate.GetMessage().Contains("itself", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("SelfBindingLoop.cui.xml", diagnostic.Location.GetLineSpan().Path);
    }

    [Fact]
    public void MarkupBindingStageZero_TemplatePartBindingReconnectsAfterTemplateSwap()
    {
        const string markup = """
            <StackPanel>
              <Button Name="Host">
                @template { <Border Name="Chrome" IsEnabled="True" /> }
              </Button>
              <TextBlock IsEnabled="$Host.parts.$Chrome.IsEnabled:OneWay" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator(
            "TemplatePartBinding.cui.xml",
            markup,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly assembly = EmitBindingTestAssembly(compilation);
        StackPanel panel = Assert.IsType<StackPanel>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi.TemplatePartBindingFactory"));
        Button host = Assert.IsType<Button>(panel.VisualChildren[0]);
        TextBlock target = Assert.IsType<TextBlock>(panel.VisualChildren[1]);
        Border oldChrome = Assert.IsType<Border>(host.ComponentTemplateInstance!.Parts["Chrome"]);
        Assert.True(target.IsEnabled);

        oldChrome.IsEnabled = false;
        Assert.False(target.IsEnabled);
        host.ComponentTemplate = new ComponentTemplate<Button>(
            "replacement",
            context =>
            {
                Border replacement = new() { IsEnabled = true };
                context.RequirePart("Chrome", replacement);
                return replacement;
            });
        Border newChrome = Assert.IsType<Border>(host.ComponentTemplateInstance!.Parts["Chrome"]);
        Assert.True(target.IsEnabled);
        oldChrome.IsEnabled = true;
        oldChrome.IsEnabled = false;
        Assert.True(target.IsEnabled);
        newChrome.IsEnabled = false;
        Assert.False(target.IsEnabled);
    }

    [Fact]
    public void MarkupBindingStageZero_ConditionalBindingsActivateRestoreAndGateTwoWayWrites()
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;
            public sealed class ConditionalViewModel : INotifyPropertyChanged
            {
                private bool useShort = true;
                private bool useLong;
                private string shortName = "Scurt";
                private string longName = "Nume lung";
                public event PropertyChangedEventHandler? PropertyChanged;
                public bool UseShort { get => useShort; set { useShort = value; Changed(nameof(UseShort)); } }
                public bool UseLong { get => useLong; set { useLong = value; Changed(nameof(UseLong)); } }
                public string ShortName { get => shortName; set { shortName = value; Changed(nameof(ShortName)); } }
                public string LongName { get => longName; set { longName = value; Changed(nameof(LongName)); } }
                private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            """;
        const string markup = """
            <StackPanel DataType="TestInput.ConditionalViewModel">
              <TextBlock Text="Base implicit">
                @when $DataContext.UseShort { Text = $DataContext.ShortName; }
              </TextBlock>
              <TextBlock Text="Base explicit">
                @when $DataContext.UseShort { Text = $DataContext.ShortName:OneWay; }
              </TextBlock>
              <TextBox Text="Base editor">
                @when $DataContext.UseShort { Text = $DataContext.ShortName:TwoWay; }
                @when $DataContext.UseLong { Text = $DataContext.LongName:TwoWay; }
              </TextBox>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "ConditionalBinding.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly assembly = EmitBindingTestAssembly(compilation);
        Type viewModelType = assembly.GetType("TestInput.ConditionalViewModel", throwOnError: true)!;
        object viewModel = Activator.CreateInstance(viewModelType)!;
        StackPanel panel = Assert.IsType<StackPanel>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi.ConditionalBindingFactory",
            viewModel));
        TextBlock implicitOneWay = Assert.IsType<TextBlock>(panel.VisualChildren[0]);
        TextBlock explicitOneWay = Assert.IsType<TextBlock>(panel.VisualChildren[1]);
        TextBox editor = Assert.IsType<TextBox>(panel.VisualChildren[2]);
        Assert.Equal("Scurt", implicitOneWay.Text);
        Assert.Equal("Scurt", explicitOneWay.Text);
        Assert.Equal("Scurt", editor.Text);

        viewModelType.GetProperty("ShortName")!.SetValue(viewModel, "Actualizat");
        Assert.Equal("Actualizat", implicitOneWay.Text);
        Assert.Equal("Actualizat", explicitOneWay.Text);
        Assert.Equal("Actualizat", editor.Text);
        editor.Text = "Scris scurt";
        Assert.Equal("Scris scurt", viewModelType.GetProperty("ShortName")!.GetValue(viewModel));

        viewModelType.GetProperty("UseShort")!.SetValue(viewModel, false);
        Assert.Equal("Base implicit", implicitOneWay.Text);
        Assert.Equal("Base explicit", explicitOneWay.Text);
        Assert.Equal("Base editor", editor.Text);
        viewModelType.GetProperty("UseLong")!.SetValue(viewModel, true);
        Assert.Equal("Nume lung", editor.Text);
        viewModelType.GetProperty("ShortName")!.SetValue(viewModel, "Nu castiga");
        Assert.Equal("Nume lung", editor.Text);
        editor.Text = "Scris lung";
        Assert.Equal("Scris lung", viewModelType.GetProperty("LongName")!.GetValue(viewModel));
        Assert.Equal("Nu castiga", viewModelType.GetProperty("ShortName")!.GetValue(viewModel));
        viewModelType.GetProperty("UseLong")!.SetValue(viewModel, false);
        Assert.Equal("Base editor", editor.Text);
    }

    [Theory]
    [InlineData("$DataContext.ShortName")]
    [InlineData("$DataContext.ShortName:OneWay")]
    [InlineData("$DataContext.ShortName:TwoWay")]
    public void MarkupBindingStageZero_QuotedConditionalPathIsRejectedWhileOrdinaryLiteralStaysLegal(string quotedPath)
    {
        const string literalMarkup = """
            <TextBlock IsEnabled="True" Text="Base">
              @when IsEnabled { Text = "MyText"; }
            </TextBlock>
            """;
        GeneratorRunResult literal = RunGenerator(
            "ConditionalLiteral.cui.xml",
            literalMarkup,
            out Compilation literalCompilation);
        Assert.DoesNotContain(literal.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly literalAssembly = EmitBindingTestAssembly(literalCompilation);
        TextBlock literalText = Assert.IsType<TextBlock>(InvokeBindingTestCreate(
            literalAssembly,
            "Cerneala.GeneratedUi.ConditionalLiteralFactory"));
        Assert.Equal("MyText", literalText.Text);

        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;
            public sealed class QuotedViewModel : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public string ShortName { get; set; } = "Scurt";
            }
            """;
        string invalidMarkup = $$"""
            <TextBlock DataType="TestInput.QuotedViewModel" IsEnabled="True" Text="Base">
              @when IsEnabled { Text = "{{quotedPath}}"; }
            </TextBlock>
            """;
        GeneratorRunResult invalid = RunGeneratorWithInput(
            "QuotedConditionalBinding.cui.xml",
            invalidMarkup,
            inputSource,
            out _);
        Diagnostic diagnostic = Assert.Single(
            invalid.Diagnostics,
            candidate => candidate.Severity == DiagnosticSeverity.Error &&
                candidate.GetMessage().Contains("unquoted", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("QuotedConditionalBinding.cui.xml", diagnostic.Location.GetLineSpan().Path);
    }

    [Fact]
    public void MarkupBindingStageZero_ExistingOwnerResourcesAndLogicalSourcesRemainCompatible()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <SolidColorBrush Name="Accent" Color="White" />
              </StackPanel.Resources>
              <Button Content="Owner">
                @template { <ContentPresenter Content="$owner.Content" /> }
              </Button>
              <TextBlock Text="Base" Background="$Accent" IsEnabled="False" IsVisible="False">
                @when (IsEnabled and IsVisible) or IsMouseOver { Text = "Active"; }
              </TextBlock>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator(
            "BindingCompatibility.cui.xml",
            markup,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains(".Bind(global::Cerneala.UI.Controls.ContentControl.ContentProperty", SingleGeneratedSource(result));
        Assembly assembly = EmitBindingTestAssembly(compilation);
        StackPanel panel = Assert.IsType<StackPanel>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi.BindingCompatibilityFactory"));
        Button button = Assert.IsType<Button>(panel.VisualChildren[0]);
        TextBlock text = Assert.IsType<TextBlock>(panel.VisualChildren[1]);
        ContentPresenter presenter = Assert.IsType<ContentPresenter>(button.ComponentTemplateInstance!.Root);
        Assert.Equal("Owner", presenter.Content);
        Assert.Equal("Base", text.Text);

        button.Content = "Changed";
        Assert.Equal("Changed", presenter.Content);
        text.IsPointerOver = true;
        Assert.Equal("Active", text.Text);
        text.IsEnabled = true;
        text.IsPointerOver = false;
        Assert.Equal("Base", text.Text);
        text.IsVisible = true;
        Assert.Equal("Active", text.Text);
    }

    [Fact]
    public void MarkupBindingStageZero_ExplicitOwnerOneWayMatchesImplicitTemplateBinding()
    {
        const string implicitMarkup = """
            <Button Content="Owner">
              @template { <ContentPresenter Content="$owner.Content" /> }
            </Button>
            """;
        const string explicitMarkup = """
            <Button Content="Owner">
              @template { <ContentPresenter Content="$owner.Content:OneWay" /> }
            </Button>
            """;

        GeneratorRunResult implicitResult = RunGenerator(
            "ImplicitOwnerBinding.cui.xml",
            implicitMarkup,
            out Compilation implicitCompilation);
        GeneratorRunResult explicitResult = RunGenerator(
            "ExplicitOwnerBinding.cui.xml",
            explicitMarkup,
            out Compilation explicitCompilation);
        Assert.DoesNotContain(implicitResult.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(explicitResult.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Equal(
            Count(SingleGeneratedSource(implicitResult), ".Bind("),
            Count(SingleGeneratedSource(explicitResult), ".Bind("));

        Assembly implicitAssembly = EmitBindingTestAssembly(implicitCompilation);
        Assembly explicitAssembly = EmitBindingTestAssembly(explicitCompilation);
        Button implicitButton = Assert.IsType<Button>(InvokeBindingTestCreate(
            implicitAssembly,
            "Cerneala.GeneratedUi.ImplicitOwnerBindingFactory"));
        Button explicitButton = Assert.IsType<Button>(InvokeBindingTestCreate(
            explicitAssembly,
            "Cerneala.GeneratedUi.ExplicitOwnerBindingFactory"));
        ContentPresenter implicitPresenter = Assert.IsType<ContentPresenter>(implicitButton.ComponentTemplateInstance!.Root);
        ContentPresenter explicitPresenter = Assert.IsType<ContentPresenter>(explicitButton.ComponentTemplateInstance!.Root);
        implicitButton.Content = "Implicit changed";
        explicitButton.Content = "Explicit changed";
        Assert.Equal("Implicit changed", implicitPresenter.Content);
        Assert.Equal("Explicit changed", explicitPresenter.Content);
    }

    private static Assembly EmitBindingTestAssembly(Compilation compilation)
    {
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        return Assembly.Load(stream.ToArray());
    }

    private static UIElement InvokeBindingTestCreate(Assembly assembly, string typeName)
    {
        Type type = assembly.GetType(typeName, throwOnError: true)!;
        MethodInfo method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == "Create" && candidate.GetParameters().Length == 0);
        return Assert.IsAssignableFrom<UIElement>(method.Invoke(null, null));
    }

    private static UIElement InvokeBindingTestCreate(Assembly assembly, string typeName, object dataContext)
    {
        Type type = assembly.GetType(typeName, throwOnError: true)!;
        MethodInfo method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == "Create" && candidate.GetParameters().Length == 1);
        return Assert.IsAssignableFrom<UIElement>(method.Invoke(null, new[] { dataContext }));
    }
}
