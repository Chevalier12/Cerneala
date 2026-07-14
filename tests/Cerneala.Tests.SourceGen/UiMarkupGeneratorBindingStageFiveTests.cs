using System;
using System.Linq;
using System.Reflection;
using Cerneala.UI.Controls;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    private const string BindingStageFiveInputSource = """
        using System.ComponentModel;
        namespace TestInput;

        public sealed class BindingViewModel : INotifyPropertyChanged
        {
            private string name = "Ana";
            private int count = 3;
            private bool flag = true;
            public event PropertyChangedEventHandler? PropertyChanged;
            public string Name { get => name; set { name = value; Raise(nameof(Name)); } }
            public string ReadOnlyName { get; } = "Read only";
            public int Count { get => count; set { count = value; Raise(nameof(Count)); } }
            public float Offset { get; set; } = 4;
            public bool Flag { get => flag; set { flag = value; Raise(nameof(Flag)); } }
            public PlainChild Plain { get; } = new();
            private string Secret { get; } = "Hidden";
            private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public sealed class PlainChild
        {
            public string Name { get; set; } = "Static";
        }
        """;

    [Fact]
    public void BindingStageFive_SyntaxAndTemplatePartDiagnosticsAreActionable()
    {
        (string File, string Markup, string Message)[] cases =
        [
            (
                "UnknownMode.cui.xml",
                "<TextBlock DataType=\"TestInput.BindingViewModel\" Text=\"$DataContext.Name:Sideways\" />",
                "Unknown binding mode"),
            (
                "EmptyPath.cui.xml",
                "<TextBlock DataType=\"TestInput.BindingViewModel\" Text=\"$DataContext.\" />",
                "path token"),
            (
                "MissingPartProperty.cui.xml",
                """
                <StackPanel>
                  <Button Name="Host">@template { <Border Name="Chrome" /> }</Button>
                  <TextBlock Text="$Host.parts.$Chrome" />
                </StackPanel>
                """,
                "$control.parts.$part.Property"),
            (
                "CapitalizedParts.cui.xml",
                """
                <StackPanel>
                  <Button Name="Host">@template { <Border Name="Chrome" /> }</Button>
                  <TextBlock IsEnabled="$Host.Parts.$Chrome.IsEnabled" />
                </StackPanel>
                """,
                "lowercase")
        ];

        foreach ((string file, string markup, string message) in cases)
        {
            Diagnostic diagnostic = BindingStageFiveSingleError(file, markup);
            Assert.Contains(message, diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
            Assert.Equal(file, diagnostic.Location.GetLineSpan().Path);
        }
    }

    [Fact]
    public void BindingStageFive_SemanticDiagnosticsCoverTypesAccessAndWritability()
    {
        (string File, string Markup, string Message)[] cases =
        [
            (
                "MissingDataType.cui.xml",
                "<TextBlock Text=\"$DataContext.Name\" />",
                "DataType is required"),
            (
                "MissingMember.cui.xml",
                "<TextBlock DataType=\"TestInput.BindingViewModel\" Text=\"$DataContext.Missing\" />",
                "Readable property 'Missing' was not found"),
            (
                "InaccessibleGetter.cui.xml",
                "<TextBlock DataType=\"TestInput.BindingViewModel\" Text=\"$DataContext.Secret\" />",
                "Readable property 'Secret' was not found"),
            (
                "MismatchedType.cui.xml",
                "<TextBlock DataType=\"TestInput.BindingViewModel\" IsEnabled=\"$DataContext.Name\" />",
                "not compatible"),
            (
                "ReadOnlyTarget.cui.xml",
                "<ScrollContentPresenter DataType=\"TestInput.BindingViewModel\" HorizontalOffset=\"$DataContext.Offset\" />",
                "read-only"),
            (
                "ReadOnlySource.cui.xml",
                "<TextBox DataType=\"TestInput.BindingViewModel\" Text=\"$DataContext.ReadOnlyName:TwoWay\" />",
                "writable source endpoint"),
            (
                "InverseStringConversion.cui.xml",
                "<TextBox DataType=\"TestInput.BindingViewModel\" Text=\"$DataContext.Count:TwoWay\" />",
                "OneWay only"),
            (
                "UnobservableNestedOwner.cui.xml",
                "<TextBlock DataType=\"TestInput.BindingViewModel\" Text=\"$DataContext.Plain.Name\" />",
                "INotifyPropertyChanged")
        ];

        foreach ((string file, string markup, string message) in cases)
        {
            Diagnostic diagnostic = BindingStageFiveSingleError(file, markup);
            Assert.Contains(message, diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void BindingStageFive_NamedScopePropertyAndSelfDiagnosticsArePrecise()
    {
        (string File, string Markup, string Message)[] cases =
        [
            (
                "MissingNamedElement.cui.xml",
                "<TextBlock Text=\"$Missing.Text\" />",
                "Unknown named element"),
            (
                "MissingNamedProperty.cui.xml",
                "<StackPanel><TextBlock Name=\"Source\" /><TextBlock Text=\"$Source.Missing\" /></StackPanel>",
                "No supported UI property"),
            (
                "ReadOnlyNamedSource.cui.xml",
                "<StackPanel><ScrollContentPresenter Name=\"Source\" /><ProgressBar Value=\"$Source.HorizontalOffset:TwoWay\" /></StackPanel>",
                "writable source endpoint"),
            (
                "ReadOnlyTemplatePart.cui.xml",
                """
                <StackPanel>
                  <Button Name="Host">@template { <ScrollContentPresenter Name="ReadOnlyPart" /> }</Button>
                  <ProgressBar Value="$Host.parts.$ReadOnlyPart.HorizontalOffset:TwoWay" />
                </StackPanel>
                """,
                "writable source endpoint"),
            (
                "DirectSelf.cui.xml",
                "<TextBlock Text=\"$self.Text\" />",
                "cannot bind directly to itself"),
            (
                "NamedSelf.cui.xml",
                "<TextBlock Name=\"Label\" Text=\"$Label.Text\" />",
                "cannot bind directly to itself"),
            (
                "OutsideTemplateScope.cui.xml",
                """
                <StackPanel>
                  <TextBlock Name="Outside" Text="Outer" />
                  <Button>@template { <TextBlock Text="$Outside.Text" /> }</Button>
                </StackPanel>
                """,
                "name scope")
        ];

        foreach ((string file, string markup, string message) in cases)
        {
            Diagnostic diagnostic = BindingStageFiveSingleError(file, markup);
            Assert.Contains(message, diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void BindingStageFive_ConditionModesAreRejectedButAssignmentModesRemainLegal()
    {
        foreach (string mode in new[] { "OneWay", "TwoWay" })
        {
            string invalidMarkup = $$"""
                <TextBlock DataType="TestInput.BindingViewModel" Text="Base">
                  @when $DataContext.Flag:{{mode}} { Text = "Active"; }
                </TextBlock>
                """;
            Diagnostic diagnostic = BindingStageFiveSingleError("Condition" + mode + ".cui.xml", invalidMarkup);
            Assert.Contains("not allowed", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("condition", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        }

        const string validMarkup = """
            <TextBox DataType="TestInput.BindingViewModel" Text="Base">
              @when $DataContext.Flag { Text = $DataContext.Name:TwoWay; }
            </TextBox>
            """;
        GeneratorRunResult valid = RunGeneratorWithInput(
            "ConditionalAssignmentMode.cui.xml",
            validMarkup,
            BindingStageFiveInputSource,
            out _);
        Assert.DoesNotContain(valid.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void BindingStageFive_AssignmentAndInterpolationDiagnosticsDistinguishLegalText()
    {
        foreach (string quotedPath in new[]
        {
            "$DataContext.Name",
            "$DataContext.Name:OneWay",
            "$DataContext.Name:TwoWay"
        })
        {
            string markup = $$"""
                <TextBlock DataType="TestInput.BindingViewModel" IsEnabled="True" Text="Base">
                  @when IsEnabled { Text = "{{quotedPath}}"; }
                </TextBlock>
                """;
            Diagnostic diagnostic = BindingStageFiveSingleError("QuotedAssignment.cui.xml", markup);
            Assert.Contains("unquoted", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        }

        foreach (string mode in new[] { "OneWay", "TwoWay", "Sideways" })
        {
            string markup = "<TextBlock DataType=\"TestInput.BindingViewModel\" Text=\"Hello $DataContext.Name:" + mode + "\" />";
            Diagnostic diagnostic = BindingStageFiveSingleError("Interpolation" + mode + ".cui.xml", markup);
            Assert.Contains(
                mode == "Sideways" ? "Unknown binding mode" : "interpolated string",
                diagnostic.GetMessage(),
                StringComparison.OrdinalIgnoreCase);
        }

        Diagnostic missingSemicolon = BindingStageFiveSingleError(
            "MissingAssignmentSemicolon.cui.xml",
            """
            <TextBlock DataType="TestInput.BindingViewModel" IsEnabled="True" Text="Base">
              @when IsEnabled { Text = $DataContext.Name }
            </TextBlock>
            """);
        Assert.Contains(";", missingSemicolon.GetMessage(), StringComparison.Ordinal);

        Diagnostic trailing = BindingStageFiveSingleError(
            "TrailingAssignmentBinding.cui.xml",
            """
            <TextBlock DataType="TestInput.BindingViewModel" IsEnabled="True" Text="Base">
              @when IsEnabled { Text = $DataContext.Name:OneWay trailing; }
            </TextBlock>
            """);
        Assert.Contains("path token", trailing.GetMessage(), StringComparison.OrdinalIgnoreCase);

        const string legalMarkup = """
            <TextBlock DataType="TestInput.BindingViewModel" IsEnabled="True" Text="Salut, lume: 10:30">
              @when IsEnabled { Text = "MyText"; }
              @when $DataContext.Flag { Text = "Salut, $DataContext.Name"; }
            </TextBlock>
            """;
        GeneratorRunResult legal = RunGeneratorWithInput(
            "LegalLiteralAndInterpolation.cui.xml",
            legalMarkup,
            BindingStageFiveInputSource,
            out _);
        Assert.DoesNotContain(legal.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void BindingStageFive_ResourcesAspectsAndColonLiteralsAreNotBindings()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <SolidColorBrush Name="Accent" Color="White" />
                <Aspect Name="NamedAspect" Target="TextBlock">
                  @default { IsEnabled = False; }
                </Aspect>
              </StackPanel.Resources>
              <TextBlock Aspect="$NamedAspect" Background="$Accent" Text="Ora: 10:30" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator(
            "BindingResourceCompatibility.cui.xml",
            markup,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.True(compilation.GetDiagnostics().All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error));
        string generated = SingleGeneratedSource(result);
        Assert.DoesNotContain("AttachPropertyBinding", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("AttachInterpolatedStringBinding", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void BindingStageFive_InterpolationPunctuationProjectionAndEscapesStayReactive()
    {
        const string markup = """
            <StackPanel DataType="TestInput.BindingViewModel">
              <TextBlock Text="$DataContext.Count" />
              <TextBlock Text="Salut: $DataContext.Name, count=[$DataContext.Count]; repeat=$DataContext.Name/$DataContext.Name; pret $ 10" />
              <TextBlock Text="Salut, lume" />
              <TextBlock Text="\$DataContext.Name:TwoWay" />
              <TextBlock IsEnabled="True" Text="Base">
                @when IsEnabled { Text = "Escaped \$DataContext.Name:OneWay"; }
              </TextBlock>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "BindingInterpolationCompatibility.cui.xml",
            markup,
            BindingStageFiveInputSource,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly assembly = EmitBindingTestAssembly(compilation);
        Type viewModelType = assembly.GetType("TestInput.BindingViewModel", throwOnError: true)!;
        object viewModel = Activator.CreateInstance(viewModelType)!;
        StackPanel panel = Assert.IsType<StackPanel>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi.BindingInterpolationCompatibilityFactory",
            viewModel));
        TextBlock[] texts = panel.VisualChildren.Cast<TextBlock>().ToArray();

        Assert.Equal("3", texts[0].Text);
        Assert.Equal("Salut: Ana, count=[3]; repeat=Ana/Ana; pret $ 10", texts[1].Text);
        Assert.Equal("Salut, lume", texts[2].Text);
        Assert.Equal("$DataContext.Name:TwoWay", texts[3].Text);
        Assert.Equal("Escaped $DataContext.Name:OneWay", texts[4].Text);

        viewModelType.GetProperty("Name")!.SetValue(viewModel, "Mara");
        viewModelType.GetProperty("Count")!.SetValue(viewModel, 7);
        Assert.Equal("7", texts[0].Text);
        Assert.Equal("Salut: Mara, count=[7]; repeat=Mara/Mara; pret $ 10", texts[1].Text);
        Assert.Equal("$DataContext.Name:TwoWay", texts[3].Text);
        Assert.Equal("Escaped $DataContext.Name:OneWay", texts[4].Text);
    }

    [Fact]
    public void BindingStageFive_OwnerAndImplicitOneWayFormsRemainEquivalent()
    {
        const string markup = """
            <StackPanel DataType="TestInput.BindingViewModel">
              <Slider Name="Volume" Value="40" />
              <ProgressBar Value="$Volume.Value" />
              <ProgressBar Value="$Volume.Value:OneWay" />
              <TextBlock IsEnabled="True" Text="Base">
                @when IsEnabled { Text = $DataContext.Name; }
                @when IsVisible { Text = $DataContext.Name:OneWay; }
              </TextBlock>
              <Button Content="Owner">
                @template { <StackPanel><ContentPresenter Content="$owner.Content" /><ContentPresenter Content="$owner.Content:OneWay" /></StackPanel> }
              </Button>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "BindingImplicitExplicitCompatibility.cui.xml",
            markup,
            BindingStageFiveInputSource,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string generated = SingleGeneratedSource(result);
        Assert.Equal(2, Count(generated, "AttachPropertyBinding<"));
        Assert.Equal(2, Count(generated, "CreateConditionalPropertyBinding<"));
        Assert.Equal(2, Count(generated, ".Bind(global::Cerneala.UI.Controls.ContentControl.ContentProperty"));
        Assert.True(compilation.GetDiagnostics().All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error));

        Diagnostic ownerTwoWay = BindingStageFiveSingleError(
            "OwnerTwoWay.cui.xml",
            "<Button>@template { <ContentPresenter Content=\"$owner.Content:TwoWay\" /> }</Button>");
        Assert.Contains("TwoWay", ownerTwoWay.GetMessage(), StringComparison.Ordinal);

        GeneratorRunResult bareNamedElement = RunGenerator(
            "BareNamedElement.cui.xml",
            "<StackPanel><Slider Name=\"Volume\" /><ProgressBar Value=\"$Volume\" /></StackPanel>",
            out _);
        Assert.Contains(
            bareNamedElement.Diagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(
            bareNamedElement.GeneratedSources,
            source => source.SourceText.ToString().Contains("AttachPropertyBinding", StringComparison.Ordinal));
    }

    [Fact]
    public void BindingStageFive_OwnerTemplateBindingIsRestoredAfterConditionalOverride()
    {
        const string markup = """
            <Button Content="Base" IsEnabled="True">
              @template
              {
                <ContentPresenter Content="$owner.Content">
                  @when $owner.IsEnabled { Content = "Conditional"; }
                </ContentPresenter>
              }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator(
            "OwnerConditionalRestore.cui.xml",
            markup,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly assembly = EmitBindingTestAssembly(compilation);
        Button button = Assert.IsType<Button>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi.OwnerConditionalRestoreFactory"));
        ContentPresenter presenter = Assert.IsType<ContentPresenter>(button.ComponentTemplateInstance!.Root);

        Assert.Equal("Conditional", presenter.Content);
        button.IsEnabled = false;
        Assert.Equal("Base", presenter.Content);
        button.Content = "Changed";
        Assert.Equal("Changed", presenter.Content);
        button.IsEnabled = true;
        Assert.Equal("Conditional", presenter.Content);
        button.IsEnabled = false;
        Assert.Equal("Changed", presenter.Content);
    }

    [Fact]
    public void BindingStageFive_TemplatePartRequiresTerminalPropertyInAttributeAndCondition()
    {
        const string attributeMarkup = """
            <StackPanel>
              <Button Name="Host">@template { <Border Name="Chrome" /> }</Button>
              <TextBlock Text="$Host.parts.$Chrome" />
            </StackPanel>
            """;
        const string conditionMarkup = """
            <StackPanel>
              <Button Name="Host">@template { <Border Name="Chrome" /> }</Button>
              <TextBlock Text="Base">@when $Host.parts.$Chrome { Text = "Active"; }</TextBlock>
            </StackPanel>
            """;

        foreach ((string file, string markup) in new[]
        {
            ("PartWithoutPropertyAttribute.cui.xml", attributeMarkup),
            ("PartWithoutPropertyCondition.cui.xml", conditionMarkup)
        })
        {
            Diagnostic diagnostic = BindingStageFiveSingleError(file, markup);
            Assert.Contains("$control.parts.$part.Property", diagnostic.GetMessage(), StringComparison.Ordinal);
        }
    }

    private static Diagnostic BindingStageFiveSingleError(string fileName, string markup)
    {
        GeneratorRunResult result = RunGeneratorWithInput(
            fileName,
            markup,
            BindingStageFiveInputSource,
            out _);
        return Assert.Single(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }
}
