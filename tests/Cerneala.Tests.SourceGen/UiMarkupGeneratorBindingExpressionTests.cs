using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    private const string BindingExpressionInputSource = """
        using System.ComponentModel;
        namespace TestInput;
        public sealed class ExpressionViewModel : INotifyPropertyChanged
        {
            private PropertyChangedEventHandler? changed;
            private bool hasName;
            private bool isValid;
            private bool active;
            private int count;
            private string other = "";
            public event PropertyChangedEventHandler? PropertyChanged
            {
                add { changed += value; SubscriberCount++; }
                remove { changed -= value; SubscriberCount--; }
            }
            public int SubscriberCount { get; private set; }
            public int HasNameReads { get; private set; }
            public bool HasName { get { HasNameReads++; return hasName; } set { hasName = value; Raise(nameof(HasName)); } }
            public bool IsValid { get => isValid; set { isValid = value; Raise(nameof(IsValid)); } }
            public bool Active { get => active; set { active = value; Raise(nameof(Active)); } }
            public int Count { get => count; set { count = value; Raise(nameof(Count)); } }
            public string Other { get => other; set { other = value; Raise(nameof(Other)); } }
            public string Name { get; set; } = "Ana";
            private void Raise(string name) => changed?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        """;

    [Fact]
    public void BindingExpression_BoolTargetWithTwoSourcesEmitsOneObservationPerSourceAndComputedBinding()
    {
        const string markup = """
            <StackPanel DataType="TestInput.ExpressionViewModel">
              <TextBlock IsEnabled="$DataContext.HasName and $DataContext.IsValid or $DataContext.HasName" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "BindingExpressionEmission.crn",
            markup,
            BindingExpressionInputSource,
            out Compilation compilation);
        string generated = SingleGeneratedSource(result);

        Assert.Empty(result.Diagnostics);
        Assert.Contains("AttachComputedBinding<bool>(", generated, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(generated, "MarkupDataPathSegment\\(\"HasName\"").Count);
        Assert.Equal(1, Regex.Matches(generated, "MarkupDataPathSegment\\(\"IsValid\"").Count);
        Assert.Equal(2, Regex.Matches(generated, "MarkupObservation observation\\d+ = ").Count);
        Assert.True(compilation.GetDiagnostics().All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error));
    }

    [Fact]
    public void BindingExpression_RecomputesInTheSameFrameAndIgnoresUnrelatedProperties()
    {
        const string markup = """
            <StackPanel DataType="TestInput.ExpressionViewModel">
              <TextBlock IsEnabled="$DataContext.HasName and $DataContext.IsValid" />
            </StackPanel>
            """;
        (Assembly assembly, object viewModel, StackPanel panel) = CreateBindingExpressionView("BindingExpressionRuntime", markup);
        Type viewModelType = viewModel.GetType();
        TextBlock target = Assert.IsType<TextBlock>(panel.VisualChildren[0]);
        int enabledChanges = 0;
        target.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, UIElement.IsEnabledProperty))
            {
                enabledChanges++;
            }
        };
        UIRoot root = new();
        root.VisualChildren.Add(panel);
        root.ProcessFrame();

        Assert.False(target.IsEnabled);
        SetViewModel(viewModel, "HasName", true);
        root.ProcessFrame();
        Assert.False(target.IsEnabled);
        SetViewModel(viewModel, "IsValid", true);
        root.ProcessFrame();
        Assert.True(target.IsEnabled);
        SetViewModel(viewModel, "HasName", false);
        root.ProcessFrame();
        Assert.False(target.IsEnabled);

        int changesBefore = enabledChanges;
        int readsBefore = (int)viewModelType.GetProperty("HasNameReads")!.GetValue(viewModel)!;
        SetViewModel(viewModel, "Other", "unrelated");
        root.ProcessFrame();
        Assert.Equal(changesBefore, enabledChanges);
        Assert.Equal(readsBefore, (int)viewModelType.GetProperty("HasNameReads")!.GetValue(viewModel)!);
        GC.KeepAlive(assembly);
    }

    [Fact]
    public void BindingExpression_DetachAndTemplateDisposeStopEveryObservation()
    {
        const string markup = """
            <StackPanel DataType="TestInput.ExpressionViewModel">
              <TextBlock IsEnabled="$DataContext.HasName and $DataContext.IsValid" />
            </StackPanel>
            """;
        (_, object viewModel, StackPanel panel) = CreateBindingExpressionView("BindingExpressionDetach", markup);
        TextBlock target = Assert.IsType<TextBlock>(panel.VisualChildren[0]);
        UIRoot root = new();
        root.VisualChildren.Add(panel);
        root.ProcessFrame();
        Assert.Equal(2, SubscriberCount(viewModel));

        root.VisualChildren.Remove(panel);
        Assert.Equal(0, SubscriberCount(viewModel));
        SetViewModel(viewModel, "HasName", true);
        SetViewModel(viewModel, "IsValid", true);
        root.ProcessFrame();
        Assert.False(target.IsEnabled);

        root.VisualChildren.Add(panel);
        root.ProcessFrame();
        Assert.Equal(2, SubscriberCount(viewModel));
        Assert.True(target.IsEnabled);

        const string templateMarkup = """
            <Button DataType="TestInput.ExpressionViewModel">
              @template { <TextBlock IsEnabled="$DataContext.HasName and $DataContext.IsValid" /> }
            </Button>
            """;
        GeneratorRunResult result = RunGeneratorWithInput(
            "BindingExpressionTemplate.crn",
            templateMarkup,
            BindingExpressionInputSource,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assembly assembly = EmitBindingStageFourAssembly(compilation);
        object templateViewModel = Activator.CreateInstance(
            assembly.GetType("TestInput.ExpressionViewModel", throwOnError: true)!)!;
        Button button = Assert.IsType<Button>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi.BindingExpressionTemplateFactory",
            templateViewModel));
        SetViewModel(templateViewModel, "HasName", true);
        SetViewModel(templateViewModel, "IsValid", true);
        UIRoot templateRoot = new();
        templateRoot.VisualChildren.Add(button);
        templateRoot.ProcessFrame();
        Assert.Equal(2, SubscriberCount(templateViewModel));

        TextBlock templateTarget = Assert.IsType<TextBlock>(button.ComponentTemplateInstance!.Root);
        Assert.True(templateTarget.IsEnabled);
        button.ComponentTemplate = new ComponentTemplate<Button>("replacement", _ => new Border());
        Assert.Equal(0, SubscriberCount(templateViewModel));

        // Disposal clears the binding's value (IsEnabled returns to its default, true). A
        // binding that still observed its sources would recompute false here.
        SetViewModel(templateViewModel, "IsValid", false);
        templateRoot.ProcessFrame();
        Assert.True(templateTarget.IsEnabled);
    }

    [Fact]
    public void BindingExpression_ConditionalAssignmentObservesOnlyWhileBranchIsActive()
    {
        const string markup = """
            <StackPanel DataType="TestInput.ExpressionViewModel">
              <TextBlock IsEnabled="True">
                @when $DataContext.Count {
                  @if value > 0 { IsEnabled = $DataContext.HasName or $DataContext.IsValid; }
                }
              </TextBlock>
            </StackPanel>
            """;
        (_, object viewModel, StackPanel panel) = CreateBindingExpressionView("BindingExpressionConditional", markup);
        TextBlock target = Assert.IsType<TextBlock>(panel.VisualChildren[0]);
        UIRoot root = new();
        root.VisualChildren.Add(panel);
        root.ProcessFrame();
        Assert.True(target.IsEnabled);
        Assert.Equal(1, SubscriberCount(viewModel));

        SetViewModel(viewModel, "Count", 1);
        root.ProcessFrame();
        Assert.Equal(3, SubscriberCount(viewModel));
        Assert.False(target.IsEnabled);
        SetViewModel(viewModel, "IsValid", true);
        root.ProcessFrame();
        Assert.True(target.IsEnabled);
        SetViewModel(viewModel, "IsValid", false);
        root.ProcessFrame();
        Assert.False(target.IsEnabled);

        SetViewModel(viewModel, "Count", 0);
        root.ProcessFrame();
        Assert.Equal(1, SubscriberCount(viewModel));
        Assert.True(target.IsEnabled);
        SetViewModel(viewModel, "HasName", false);
        SetViewModel(viewModel, "IsValid", false);
        root.ProcessFrame();
        Assert.True(target.IsEnabled);
    }

    [Fact]
    public void BindingExpression_UnresolvedSourcesMakeTheirLeafOrComparisonFalse()
    {
        const string markup = """
            <StackPanel DataType="TestInput.ExpressionViewModel">
              <TextBlock IsEnabled="$DataContext.HasName and $DataContext.IsValid" />
              <TextBlock IsEnabled="$DataContext.Count != 0" />
              <TextBlock ClipToBounds="True" IsEnabled="$DataContext.HasName or $self.ClipToBounds" />
            </StackPanel>
            """;
        (_, object viewModel, StackPanel panel) = CreateBindingExpressionView("BindingExpressionUnresolved", markup);
        TextBlock both = Assert.IsType<TextBlock>(panel.VisualChildren[0]);
        TextBlock count = Assert.IsType<TextBlock>(panel.VisualChildren[1]);
        TextBlock either = Assert.IsType<TextBlock>(panel.VisualChildren[2]);
        SetViewModel(viewModel, "HasName", true);
        SetViewModel(viewModel, "IsValid", true);
        SetViewModel(viewModel, "Count", 5);
        UIRoot root = new();
        root.VisualChildren.Add(panel);
        root.ProcessFrame();
        Assert.True(both.IsEnabled);
        Assert.True(count.IsEnabled);
        Assert.True(either.IsEnabled);

        panel.DataContext = null;
        root.ProcessFrame();
        Assert.False(both.IsEnabled);
        Assert.False(count.IsEnabled);
        Assert.True(either.IsEnabled);
        either.ClipToBounds = false;
        root.ProcessFrame();
        Assert.False(either.IsEnabled);
    }

    [Fact]
    public void BindingExpression_SingleSourceComparisonIsAComputedBinding()
    {
        const string markup = """
            <StackPanel DataType="TestInput.ExpressionViewModel">
              <TextBlock IsVisible="$DataContext.Count == 0" />
            </StackPanel>
            """;
        (_, object viewModel, StackPanel panel) = CreateBindingExpressionView("BindingExpressionComparison", markup);
        TextBlock target = Assert.IsType<TextBlock>(panel.VisualChildren[0]);
        UIRoot root = new();
        root.VisualChildren.Add(panel);
        root.ProcessFrame();
        Assert.True(target.IsVisible);
        SetViewModel(viewModel, "Count", 3);
        root.ProcessFrame();
        Assert.False(target.IsVisible);
        SetViewModel(viewModel, "Count", 0);
        root.ProcessFrame();
        Assert.True(target.IsVisible);
    }

    [Theory]
    [InlineData("<TextBlock Name=\"Other\" /><TextBlock IsEnabled=\"$Other.IsEnabled and $DataContext.IsValid\" />")]
    [InlineData("<TextBlock IsEnabled=\"$root.IsEnabled and $DataContext.IsValid\" />")]
    public void BindingExpression_NamedAndRootPropertyLeavesAreAccepted(string element)
    {
        string markup = "<StackPanel DataType=\"TestInput.ExpressionViewModel\">" + element + "</StackPanel>";

        GeneratorRunResult result = RunGeneratorWithInput(
            "BindingExpressionLeaves.crn",
            markup,
            BindingExpressionInputSource,
            out Compilation compilation);

        Assert.Empty(result.Diagnostics);
        Assert.Contains("AttachComputedBinding<bool>(", SingleGeneratedSource(result), StringComparison.Ordinal);
        Assert.True(compilation.GetDiagnostics().All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error));
    }

    [Theory]
    [InlineData("<TextBlock IsEnabled=\"$DataContext.Name and $DataContext.IsValid\" />", "Logical expression leaves must be Boolean.")]
    [InlineData("<TextBlock IsEnabled=\"$DataContext.Count == $DataContext.Name\" />", "Both expressions must have the same type.")]
    [InlineData("<TextBlock IsEnabled=\"$DataContext.HasName:TwoWay and $DataContext.IsValid\" />", "Binding modes are not allowed in a property expression")]
    [InlineData("<TextBlock IsEnabled=\"($DataContext.HasName and $DataContext.IsValid):OneWay\" />", "Binding modes are not allowed in a property expression")]
    [InlineData("<TextBlock Text=\"$DataContext.HasName and $DataContext.IsValid\" />", "Logical expressions are supported only for bool targets")]
    [InlineData("<TextBlock Opacity=\"$DataContext.HasName and $DataContext.IsValid\" />", "Logical expressions are supported only for bool targets")]
    [InlineData("<TextBlock IsEnabled=\"value and $DataContext.IsValid\" />", "'value' is available only inside @if.")]
    [InlineData("<TextBlock IsEnabled=\"IsFocused and $DataContext.IsValid\" />", "Property expression sources must start with '$'")]
    [InlineData("<TextBlock IsEnabled=\"$self.IsEnabled and $DataContext.IsValid\" />", "A UI property expression cannot read its own target property.")]
    [InlineData("<TextBlock Name=\"Me\" IsEnabled=\"$Me.IsEnabled or $DataContext.IsValid\" />", "A UI property expression cannot read its own target property.")]
    [InlineData("<TextBlock Name=\"Other\" /><TextBlock IsEnabled=\"$Other and $DataContext.IsValid\" />", "Logical expression leaves must be Boolean.")]
    [InlineData("<StackPanel.Resources><SolidColorBrush Name=\"Accent\" Color=\"Red\" /></StackPanel.Resources><TextBlock IsEnabled=\"$Accent and $DataContext.IsValid\" />", "Logical expression leaves must be Boolean.")]
    public void BindingExpression_InvalidExpressionsReportInvalidBindingSource(string element, string expectedMessage)
    {
        string markup = "<StackPanel DataType=\"TestInput.ExpressionViewModel\">" + element + "</StackPanel>";

        GeneratorRunResult result = RunGeneratorWithInput(
            "BindingExpressionInvalid.crn",
            markup,
            BindingExpressionInputSource,
            out _);

        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Id == "CERNEALAUI007" &&
                diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.GetMessage().Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("IsEnabled = $DataContext.Name and $DataContext.IsValid;", "Logical expression leaves must be Boolean.")]
    [InlineData("IsEnabled = $DataContext.Count == $DataContext.Name;", "Both expressions must have the same type.")]
    [InlineData("IsEnabled = $DataContext.HasName:TwoWay and $DataContext.IsValid;", "Binding modes are not allowed in a property expression")]
    [InlineData("IsEnabled = ($DataContext.HasName and $DataContext.IsValid):OneWay;", "Binding modes are not allowed in a property expression")]
    [InlineData("Text = $DataContext.HasName and $DataContext.IsValid;", "Logical expressions are supported only for bool targets; the target type is 'string'.")]
    [InlineData("Opacity = $DataContext.HasName and $DataContext.IsValid;", "Logical expressions are supported only for bool targets; the target type is 'float'.")]
    [InlineData("IsEnabled = value and $DataContext.IsValid;", "'value' is available only inside @if.")]
    [InlineData("IsEnabled = IsFocused and $DataContext.IsValid;", "Property expression sources must start with '$'")]
    [InlineData("IsEnabled = $self.IsEnabled and $DataContext.IsValid;", "A UI property expression cannot read its own target property.")]
    [InlineData("IsEnabled = $Me.IsEnabled or $DataContext.IsValid;", "A UI property expression cannot read its own target property.")]
    public void BindingExpression_InvalidConditionalAssignmentExpressionsReportInvalidBindingSource(string assignment, string expectedMessage)
    {
        string markup = "<StackPanel DataType=\"TestInput.ExpressionViewModel\"><TextBlock Name=\"Me\">@when $DataContext.Active { " +
            assignment + " }</TextBlock></StackPanel>";

        GeneratorRunResult result = RunGeneratorWithInput(
            "BindingExpressionInvalidAssignment.crn",
            markup,
            BindingExpressionInputSource,
            out _);

        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Id == "CERNEALAUI007" &&
                diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.GetMessage().Contains(expectedMessage, StringComparison.Ordinal));
    }

    private static (Assembly Assembly, object ViewModel, StackPanel Panel) CreateBindingExpressionView(string name, string markup)
    {
        GeneratorRunResult result = RunGeneratorWithInput(
            name + ".crn",
            markup,
            BindingExpressionInputSource,
            out Compilation compilation);
        Assert.Empty(result.Diagnostics);
        Assembly assembly = EmitBindingStageFourAssembly(compilation);
        object viewModel = Activator.CreateInstance(assembly.GetType("TestInput.ExpressionViewModel", throwOnError: true)!)!;
        StackPanel panel = Assert.IsType<StackPanel>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi." + name + "Factory",
            viewModel));
        return (assembly, viewModel, panel);
    }

    private static void SetViewModel(object viewModel, string property, object value)
    {
        viewModel.GetType().GetProperty(property)!.SetValue(viewModel, value);
    }

    private static int SubscriberCount(object viewModel)
    {
        return (int)viewModel.GetType().GetProperty("SubscriberCount")!.GetValue(viewModel)!;
    }
}
