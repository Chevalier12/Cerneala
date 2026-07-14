using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void BindingStageThree_CommonResolverAcceptsAllSourceKindsAndKeepsGeneratedAccessTyped()
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;

            public sealed class BindingViewModel : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Name { get; set; } = "Ana";
                public int Count { get; set; } = 3;
                public bool Flag { get; set; } = true;
                public BindingChild? Child { get; set; } = new();
            }

            public sealed class BindingChild : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Name { get; set; } = "Copil";
            }
            """;
        const string markup = """
            <StackPanel DataType="TestInput.BindingViewModel">
              <TextBlock Text="$DataContext.Name" />
              <TextBlock Text="$DataContext.Count:OneWay" />
              <TextBox Text="$DataContext.Name:TwoWay" />
              <TextBlock Text="Salut, $DataContext.Name/$DataContext.Name; copil: $DataContext.Child.Name; literal: \$DataContext.Count" />
              <ProgressBar Maximum="100" Value="$After.Value:TwoWay" />
              <Slider Name="After" Maximum="100" Value="25" />
              <TextBlock IsVisible="True" IsEnabled="$self.IsVisible" />
              <Button Name="Host" Content="Owner">
                @template { <Border Name="Chrome"><ContentPresenter Content="$owner.Content:OneWay" /></Border> }
              </Button>
              <TextBlock IsEnabled="$Host.parts.$Chrome.IsEnabled:OneWay" />
              <TextBlock Text="Base">
                @when $DataContext.Flag { Text = $DataContext.Name; }
                @when $DataContext.Flag
                { @if value == True { Text = "Conditional $DataContext.Name / $DataContext.Name"; } }
              </TextBlock>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "BindingSemanticMatrix.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("value => ((global::TestInput.BindingViewModel)value!).Flag", generated);
        Assert.Contains("(owner, value) => ((global::TestInput.BindingViewModel)owner!).Flag", generated);
        Assert.DoesNotContain("GetProperty(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Reflection", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("PropertyPath", generated, StringComparison.Ordinal);
        Assert.True(compilation.GetDiagnostics().All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error));
    }

    [Theory]
    [InlineData("$DataContext.Name:Sideways", "Unknown binding mode")]
    [InlineData("$DataContext.Name:OneWay trailing", "one unquoted path token")]
    public void BindingStageThree_DirectBindingParserRejectsInvalidFinalSuffixOrTrailingContent(
        string value,
        string message)
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;
            public sealed class ViewModel : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Name { get; set; } = "Value";
            }
            """;
        string markup = "<TextBlock DataType=\"TestInput.ViewModel\" Text=\"" + value + "\" />";

        GeneratorRunResult result = RunGeneratorWithInput(
            "InvalidBindingToken.cui.xml",
            markup,
            inputSource,
            out _);

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Severity == DiagnosticSeverity.Error);
        Assert.Contains(message, diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BindingStageThree_AssignmentParserSeparatesBindingsInterpolationsEscapesAndQuotedPurePaths()
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;
            public sealed class ViewModel : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public bool Enabled { get; set; } = true;
                public string Name { get; set; } = "Value";
            }
            """;
        const string validMarkup = """
            <TextBlock DataType="TestInput.ViewModel" Text="Base">
              @when $DataContext.Enabled { Text = $DataContext.Name; }
              @when $DataContext.Enabled
              { @if value == True { Text = "Hello $DataContext.Name, literal \$DataContext.Name"; } }
            </TextBlock>
            """;
        GeneratorRunResult valid = RunGeneratorWithInput(
            "AssignmentBindings.cui.xml",
            validMarkup,
            inputSource,
            out _);
        Assert.DoesNotContain(valid.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        const string purePathMarkup = """
            <TextBlock DataType="TestInput.ViewModel" IsEnabled="True" Text="Base">
              @when IsEnabled { Text = "$DataContext.Name"; }
            </TextBlock>
            """;
        GeneratorRunResult purePath = RunGeneratorWithInput(
            "QuotedPurePath.cui.xml",
            purePathMarkup,
            inputSource,
            out _);
        Assert.Contains(
            purePath.Diagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.GetMessage().Contains("unquoted", StringComparison.OrdinalIgnoreCase));

        const string modeInInterpolationMarkup = """
            <TextBlock DataType="TestInput.ViewModel" Text="Hello $DataContext.Name:OneWay" />
            """;
        GeneratorRunResult modeInInterpolation = RunGeneratorWithInput(
            "InterpolationMode.cui.xml",
            modeInInterpolationMarkup,
            inputSource,
            out _);
        Assert.Contains(
            modeInInterpolation.Diagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.GetMessage().Contains("interpolated", StringComparison.OrdinalIgnoreCase));

        const string unknownModeInterpolationMarkup = """
            <TextBlock DataType="TestInput.ViewModel" Text="Hello $DataContext.Name:Sideways" />
            """;
        GeneratorRunResult unknownModeInterpolation = RunGeneratorWithInput(
            "UnknownInterpolationMode.cui.xml",
            unknownModeInterpolationMarkup,
            inputSource,
            out _);
        Assert.Contains(
            unknownModeInterpolation.Diagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.GetMessage().Contains("Unknown binding mode", StringComparison.OrdinalIgnoreCase));

        const string invalidAssignmentMarkup = """
            <TextBlock DataType="TestInput.ViewModel" IsEnabled="True" Text="Base">
              @when IsEnabled { Text = $DataContext.Name:Sideways; }
            </TextBlock>
            """;
        GeneratorRunResult invalidAssignment = RunGeneratorWithInput(
            "InvalidAssignmentMode.cui.xml",
            invalidAssignmentMarkup,
            inputSource,
            out _);
        Diagnostic assignmentDiagnostic = Assert.Single(
            invalidAssignment.Diagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var assignmentPosition = assignmentDiagnostic.Location.GetLineSpan().StartLinePosition;
        string assignmentLine = invalidAssignmentMarkup.Split('\n')[1];
        Assert.Equal(1, assignmentPosition.Line);
        Assert.Equal(assignmentLine.IndexOf(":Sideways", StringComparison.Ordinal), assignmentPosition.Character);

        GeneratorRunResult literalDollar = RunGenerator(
            "LiteralDollar.cui.xml",
            "<TextBlock Text=\"Pret $ 10, literal \\$DataContext.Name\" />",
            out _);
        Assert.DoesNotContain(literalDollar.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void BindingStageThree_SemanticValidationRejectsUnobservableReadOnlyMismatchedAndSelfSources()
    {
        const string unobservableSource = """
            namespace TestInput;
            public sealed class PlainViewModel { public string Name { get; set; } = "Value"; }
            """;
        GeneratorRunResult unobservable = RunGeneratorWithInput(
            "UnobservableSemanticBinding.cui.xml",
            "<TextBlock DataType=\"TestInput.PlainViewModel\" Text=\"$DataContext.Name\" />",
            unobservableSource,
            out _);
        Assert.Contains(
            unobservable.Diagnostics,
            diagnostic => diagnostic.GetMessage().Contains("INotifyPropertyChanged", StringComparison.Ordinal));

        const string readOnlySource = """
            using System.ComponentModel;
            namespace TestInput;
            public sealed class ReadOnlyViewModel : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Name { get; } = "Value";
            }
            """;
        GeneratorRunResult readOnly = RunGeneratorWithInput(
            "ReadOnlySemanticBinding.cui.xml",
            "<TextBox DataType=\"TestInput.ReadOnlyViewModel\" Text=\"$DataContext.Name:TwoWay\" />",
            readOnlySource,
            out _);
        Assert.Contains(
            readOnly.Diagnostics,
            diagnostic => diagnostic.GetMessage().Contains("writable source", StringComparison.OrdinalIgnoreCase));

        const string mismatchSource = """
            using System.ComponentModel;
            namespace TestInput;
            public sealed class MismatchViewModel : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Name { get; set; } = "Value";
            }
            """;
        GeneratorRunResult mismatch = RunGeneratorWithInput(
            "MismatchSemanticBinding.cui.xml",
            "<TextBlock DataType=\"TestInput.MismatchViewModel\" IsEnabled=\"$DataContext.Name\" />",
            mismatchSource,
            out _);
        Assert.Contains(
            mismatch.Diagnostics,
            diagnostic => diagnostic.GetMessage().Contains("not compatible", StringComparison.OrdinalIgnoreCase));

        GeneratorRunResult self = RunGenerator(
            "SelfSemanticBinding.cui.xml",
            "<TextBlock IsEnabled=\"$self.IsEnabled\" />",
            out _);
        Assert.Contains(
            self.Diagnostics,
            diagnostic => diagnostic.GetMessage().Contains("itself", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BindingStageThree_GenericWindowAndUserControlInferDataTypeWithoutMarkupDuplication()
    {
        const string commonViewModel = """
            using System.ComponentModel;
            namespace TestInput.Views;
            public sealed class ViewModel : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Name { get; set; } = "Value";
            }
            """;
        const string userControlSource = commonViewModel + """

            namespace TestInput.Views
            {
                public partial class BindingView : Cerneala.UI.Controls.UserControl<ViewModel> { }
            }
            """;
        GeneratorRunResult userControl = RunPairedGenerator(
            "Views/BindingView.cui.xml",
            "<UserControl><TextBlock Text=\"$DataContext.Name\" /></UserControl>",
            userControlSource,
            out _);
        Assert.DoesNotContain(userControl.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        const string windowSource = commonViewModel + """

            namespace TestInput.Views
            {
                public partial class BindingWindow : Cerneala.UI.Controls.Window<ViewModel> { }
            }
            """;
        GeneratorRunResult window = RunPairedGenerator(
            "Views/BindingWindow.cui.xml",
            "<Window><TextBlock Text=\"$DataContext.Name\" /></Window>",
            windowSource,
            out _);
        Assert.DoesNotContain(window.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }
}
