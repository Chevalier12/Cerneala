using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void MotionSetGeneratesTypedImmediateAssignmentsInsideSequence()
    {
        const string markup = """
            <Border Aspect="$Stateful">
              <Border.Resources>
                <MotionClip Name="Replay" TargetType="Border">
                  @sequence
                  {
                    @set { $Status.Text = "ARMING"; Opacity = 0.5; }
                    @animate with Tween(100ms) { @to { Opacity = 1; } }
                  }
                </MotionClip>
                <Aspect Name="Stateful" TargetType="Border">
                  @on Loaded { @run $Replay; }
                </Aspect>
              </Border.Resources>
              <TextBlock Name="Status" />
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionSet.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("Status.SetValue(global::Cerneala.UI.Controls.TextBlock.TextProperty, \"ARMING\")", generated, StringComparison.Ordinal);
        Assert.Contains("GeneratedMarkup.SetMotionProperty(", generated, StringComparison.Ordinal);
        Assert.Contains("UIElement.OpacityProperty, 0.5f)", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("SetValue(global::Cerneala.UI.Elements.UIElement.OpacityProperty", generated, StringComparison.Ordinal);
        Assert.Contains("MarkupMotionExecution.Sequence", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void MotionSetFollowedByAnimateOfSamePropertyAnimatesFromTheSetValue()
    {
        // The issue's markup (#730); @when makes the document reactive, so Opacity="1" is a MarkupBase value.
        const string markup = """
            <Border Opacity="1" Aspect="$Entrance">
              <Border.Resources>
                <MotionClip Name="Replay" TargetType="Border">
                  @sequence
                  {
                    @set { $Status.Text = "ARMING"; Opacity = 0.2; }
                    @animate with Tween(100ms, Linear) { @to { Opacity = 1; } }
                  }
                </MotionClip>
                <Aspect Name="Entrance" TargetType="Border">
                  @handle Active;
                  @on Loaded { @run $Replay as Active; }
                  @when $self.IsEnabled { @if value == false { @cancel Active; } }
                </Aspect>
              </Border.Resources>
              <TextBlock Name="Status" />
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionSetThenAnimate.crn", markup, out Compilation compilation);
        AssertNoGeneratorOrCompilationErrors(result, compilation);
        using System.IO.MemoryStream stream = new();
        Microsoft.CodeAnalysis.Emit.EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        Cerneala.UI.Controls.Border border = Assert.IsType<Cerneala.UI.Controls.Border>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.MotionSetThenAnimateFactory"));
        Cerneala.UI.Controls.TextBlock status = Assert.IsType<Cerneala.UI.Controls.TextBlock>(border.Child);
        ManualClock clock = new();
        Cerneala.UI.Elements.UIRoot root = new(100, 100, motionClock: clock);
        root.VisualChildren.Add(border);
        root.ProcessFrame();

        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();

        Assert.InRange(border.Opacity, 0.55f, 0.65f);
        Assert.Equal(
            Cerneala.UI.Core.UiPropertyValueSource.Animation,
            border.GetValueSource(Cerneala.UI.Elements.UIElement.OpacityProperty));

        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();

        Assert.Equal(1f, border.Opacity, 0.001f);
        Assert.False(root.Motion.HasActiveMotion);
        Assert.Equal("ARMING", status.Text);
        Assert.Equal(
            Cerneala.UI.Core.UiPropertyValueSource.Local,
            status.GetValueSource(Cerneala.UI.Controls.TextBlock.TextProperty));

        border.Opacity = 0.5f;
        root.ProcessFrame();

        Assert.Equal(0.5f, border.Opacity, 0.001f);
        Assert.Equal(
            Cerneala.UI.Core.UiPropertyValueSource.Local,
            border.GetValueSource(Cerneala.UI.Elements.UIElement.OpacityProperty));
    }

    // #731: a literal attribute is a MarkupBase value whether or not the document has reactive
    // conditions, so a Motion animation of the same property is visible.
    [Theory]
    [InlineData("@on Loaded { @run $Replay; }")]
    [InlineData("@handle Active; @on Loaded { @run $Replay as Active; }")]
    [InlineData("@handle Active; @on Loaded { @run $Replay as Active; } @when $self.IsEnabled { @if value == false { @cancel Active; } }")]
    public void MotionClipAnimatesLiteralAttributeWithOrWithoutReactiveConditions(string aspectBody)
    {
        string markup = $$"""
            <Border Opacity="1" Aspect="$Entrance">
              <Border.Resources>
                <MotionClip Name="Replay" TargetType="Border">
                  @animate with Tween(100ms, Linear) { @from { Opacity = 1; } @to { Opacity = 0; } }
                </MotionClip>
                <Aspect Name="Entrance" TargetType="Border">
                  {{aspectBody}}
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipLiteralBase.crn", markup, out Compilation compilation);
        AssertNoGeneratorOrCompilationErrors(result, compilation);
        using System.IO.MemoryStream stream = new();
        Microsoft.CodeAnalysis.Emit.EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        Cerneala.UI.Controls.Border border = Assert.IsType<Cerneala.UI.Controls.Border>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.MotionClipLiteralBaseFactory"));

        Assert.Equal(1f, border.Opacity, 0.001f);
        Assert.Equal(
            Cerneala.UI.Core.UiPropertyValueSource.MarkupBase,
            border.GetValueSource(Cerneala.UI.Elements.UIElement.OpacityProperty));

        ManualClock clock = new();
        Cerneala.UI.Elements.UIRoot root = new(100, 100, motionClock: clock);
        root.VisualChildren.Add(border);
        root.ProcessFrame();

        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();

        Assert.Equal(0.5f, border.Opacity, 0.01f);
        Assert.Equal(
            Cerneala.UI.Core.UiPropertyValueSource.Animation,
            border.GetValueSource(Cerneala.UI.Elements.UIElement.OpacityProperty));

        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();

        Assert.Equal(0f, border.Opacity, 0.001f);
        Assert.False(root.Motion.HasActiveMotion);

        border.Opacity = 0.5f;
        root.ProcessFrame();

        Assert.Equal(0.5f, border.Opacity, 0.001f);
        Assert.Equal(
            Cerneala.UI.Core.UiPropertyValueSource.Local,
            border.GetValueSource(Cerneala.UI.Elements.UIElement.OpacityProperty));
    }

    // #731: every generated markup value of a UiProperty (literal attribute, resource reference,
    // text content) is MarkupBase in a document without reactive conditions.
    [Fact]
    public void NonReactiveDocumentWritesGeneratedMarkupValuesAsMarkupBase()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <SolidColorBrush Name="Accent" Color="White" />
              </StackPanel.Resources>
              <Border Name="Card" Opacity="0.8" Background="$Accent" />
              <TextBlock Name="Caption">Hello</TextBlock>
              <TextBlock Name="Rich"><Run Name="Part">World</Run></TextBlock>
              <Button Name="Action">Go</Button>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("NonReactiveMarkupBase.crn", markup, out Compilation compilation);
        AssertNoGeneratorOrCompilationErrors(result, compilation);
        using System.IO.MemoryStream stream = new();
        Microsoft.CodeAnalysis.Emit.EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        Cerneala.UI.Controls.StackPanel panel = Assert.IsType<Cerneala.UI.Controls.StackPanel>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.NonReactiveMarkupBaseFactory"));
        Cerneala.UI.Controls.Border card = Assert.IsType<Cerneala.UI.Controls.Border>(panel.Children[0]);
        Cerneala.UI.Controls.TextBlock caption = Assert.IsType<Cerneala.UI.Controls.TextBlock>(panel.Children[1]);
        Cerneala.UI.Controls.TextBlock rich = Assert.IsType<Cerneala.UI.Controls.TextBlock>(panel.Children[2]);
        Cerneala.UI.Documents.Run part = Assert.IsType<Cerneala.UI.Documents.Run>(Assert.Single(rich.Inlines));
        Cerneala.UI.Controls.Button action = Assert.IsType<Cerneala.UI.Controls.Button>(panel.Children[3]);

        Assert.Equal(0.8f, card.Opacity, 0.001f);
        Assert.Equal("Hello", caption.Text);
        Assert.Equal("World", part.Text);
        Assert.Equal("Go", action.Content);
        Assert.Equal(Cerneala.UI.Core.UiPropertyValueSource.MarkupBase, card.GetValueSource(Cerneala.UI.Elements.UIElement.OpacityProperty));
        Assert.Equal(Cerneala.UI.Core.UiPropertyValueSource.MarkupBase, card.GetValueSource(Cerneala.UI.Controls.Control.BackgroundProperty));
        Assert.Equal(Cerneala.UI.Core.UiPropertyValueSource.MarkupBase, caption.GetValueSource(Cerneala.UI.Controls.TextBlock.TextProperty));
        Assert.Equal(Cerneala.UI.Core.UiPropertyValueSource.MarkupBase, part.GetValueSource(Cerneala.UI.Documents.Run.TextProperty));
        Assert.Equal(Cerneala.UI.Core.UiPropertyValueSource.MarkupBase, action.GetValueSource(Cerneala.UI.Controls.ContentControl.ContentProperty));
    }

    // #731: a MarkupBase write infers SetValue's type argument from the value, so a uint literal must be typed.
    // The @when made this document emit SetValue before #731 too.
    [Fact]
    public void ReactiveDocumentCompilesUnsignedIntegerLiteralAttributes()
    {
        const string markup = """
            <StackPanel Aspect="$Gate">
              <StackPanel.Resources>
                <Aspect Name="Gate" TargetType="StackPanel">
                  @when $self.IsEnabled { @if value == false { @set { Opacity = 0.5; } } }
                </Aspect>
              </StackPanel.Resources>
              <RenderSurface2D>
                <RenderSurface2D.Scene>
                  <Scene2D>
                    <Sprite2D>
                      <BoxCollider2D Width="32" Height="8" CollisionLayer="2" CollisionMask="4294967295" />
                    </Sprite2D>
                  </Scene2D>
                </RenderSurface2D.Scene>
              </RenderSurface2D>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("ReactiveUnsignedLiteral.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        Assert.Contains(
            "Collider2D.CollisionLayerProperty, ((uint)2), global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase);",
            SingleGeneratedSource(result),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("current")]
    [InlineData("1 with Tween(100ms)")]
    public void MotionSetRejectsAnimatedOrImplicitValues(string value)
    {
        string markup = $$"""
            <Border>
              <Border.Resources>
                <MotionClip Name="Invalid" TargetType="Border">
                  @set { Opacity = {{value}}; }
                </MotionClip>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionSetInvalid.crn", markup, out _);

        AssertMotionDiagnostic(result, "concrete values");
    }

    [Fact]
    public void MotionClipExpandsNestedRecipeAtRunSiteAndResolvesNamedElementsStatically()
    {
        const string markup = """
            <Border Aspect="$Entrance">
              <Border.Resources>
                <MotionClip Name="EntranceClip" TargetType="Control">
                  @sequence
                  {
                    @animate { @to { Opacity = 1; } }
                    @parallel
                    {
                      @animate { @to { Scale = 1; } }
                      @animate { @to { $Ring.Opacity = 1; } }
                    }
                  }
                </MotionClip>
                <Aspect Name="Entrance" TargetType="Border">
                  @on Loaded { @run $EntranceClip; }
                </Aspect>
              </Border.Resources>
              <Border Name="Ring" />
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipNested.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("MarkupMotionExecution.Sequence", generated, StringComparison.Ordinal);
        Assert.Contains("MarkupMotionExecution.Parallel", generated, StringComparison.Ordinal);
        Assert.Contains("Ring", generated, StringComparison.Ordinal);
        Assert.Contains("UIElement.OpacityProperty", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("new global::Cerneala.UI.Markup.MotionClip", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("GetResource", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Dictionary", generated, StringComparison.Ordinal);
    }

    // One factory per @run in the Aspect behavior; every application runs the
    // behavior and so creates its own factory delegates and session.
    [Fact]
    public void MotionClipCreatesIndependentFactoriesForEveryRunAndAspectInstance()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <MotionClip Name="Pulse" TargetType="Button">
                  @animate { @to { Opacity = 0.5; } }
                </MotionClip>
                <Aspect Name="PulseAspect" TargetType="Button">
                  @on Loaded { @run $Pulse; }
                  @on Click { @run $Pulse; }
                </Aspect>
              </StackPanel.Resources>
              <Button Aspect="$PulseAspect" />
              <Button Aspect="$PulseAspect" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipIndependentRuns.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        string[] factories = Regex.Matches(generated, @"motionExecutionFactory\d+")
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(2, factories.Length);
        Assert.Equal(1, generated.Split("AttachMotionSession(target)", StringSplitOptions.None).Length - 1);
        Assert.Equal(2, generated.Split("StartMotionExecution", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void PairedUserControlMotionClipRequiresImportedOwnerTargetType()
    {
        const string markup = """
            <UserControl xmlns:views="clr-namespace:TestInput;assembly=GeneratorTests">
              <UserControl.Resources>
                <MotionClip Name="EntranceClip" TargetType="views:OpeningView">
                  @animate { @to { Opacity = 1; } }
                </MotionClip>
                <Aspect Name="Entrance" TargetType="views:OpeningView">
                  @on Loaded { @run $EntranceClip; }
                </Aspect>
              </UserControl.Resources>
              <Button Name="ContinueButton" />
            </UserControl>
            """;
        const string input = """
            using Cerneala.UI.Controls;

            namespace TestInput;

            internal partial class OpeningView : UserControl
            {
            }
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "OpeningView.crn",
            markup,
            input,
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        Assert.Contains("ContinueButton", SingleGeneratedSource(result), StringComparison.Ordinal);
    }

    [Fact]
    public void PairedUserControlMotionClipRejectsImplicitOwnerTargetType()
    {
        const string markup = """
            <UserControl>
              <UserControl.Resources>
                <MotionClip Name="EntranceClip" TargetType="OpeningView">
                  @animate { @to { Opacity = 1; } }
                </MotionClip>
              </UserControl.Resources>
            </UserControl>
            """;
        const string input = """
            using Cerneala.UI.Controls;

            namespace TestInput;

            internal partial class OpeningView : UserControl
            {
            }
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "OpeningView.crn",
            markup,
            input,
            out _);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.GetMessage().Contains("TargetType", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("@when IsEnabled { @animate { @to { Opacity = 1; } } }", "activation")]
    [InlineData("@on Loaded { @animate { @to { Opacity = 1; } } }", "@on")]
    [InlineData("@animate { @to { Opacity = 1; } } @animate { @to { Scale = 1; } }", "exactly one")]
    public void MotionClipRejectsActivationAndMultipleTopLevelBodies(string body, string expectedMessage)
    {
        string markup = $$"""
            <Border>
              <Border.Resources>
                <MotionClip Name="Invalid" TargetType="Border">
                  {{body}}
                </MotionClip>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipInvalidBody.crn", markup, out _);

        AssertMotionDiagnostic(result, expectedMessage);
    }

    [Fact]
    public void MotionClipRejectsRecursiveInvocation()
    {
        const string markup = """
            <Border>
              <Border.Resources>
                <MotionClip Name="Recursive" TargetType="Border">
                  @sequence
                  {
                    @animate { @to { Opacity = 1; } }
                    @run $Recursive;
                  }
                </MotionClip>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipRecursive.crn", markup, out _);

        AssertMotionDiagnostic(result, "recursive");
    }

    [Fact]
    public void MotionClipReportsMissingResource()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionClipMissing.crn",
            MotionAspectMarkup("@on Loaded { @run $Missing; }"),
            out _);

        AssertMotionDiagnostic(result, "$Missing");
    }

    [Fact]
    public void MotionClipReportsWrongTargetType()
    {
        const string markup = """
            <Button Aspect="$RunClip">
              <Button.Resources>
                <MotionClip Name="BorderClip" TargetType="Border">
                  @animate { @to { Opacity = 1; } }
                </MotionClip>
                <Aspect Name="RunClip" TargetType="Button">
                  @on Loaded { @run $BorderClip; }
                </Aspect>
              </Button.Resources>
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipWrongTarget.crn", markup, out _);

        AssertMotionDiagnostic(result, "TargetType");
    }

    [Fact]
    public void MotionClipReportsMissingNamedElementAtRunSite()
    {
        const string markup = """
            <Border Aspect="$RunClip">
              <Border.Resources>
                <MotionClip Name="NamedTargetClip" TargetType="Border">
                  @animate { @to { $Missing.Opacity = 1; } }
                </MotionClip>
                <Aspect Name="RunClip" TargetType="Border">
                  @on Loaded { @run $NamedTargetClip; }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipMissingNamedElement.crn", markup, out _);

        AssertMotionDiagnostic(result, "Missing");
    }

    [Fact]
    public void MotionClipCannotBeAssignedDirectlyToAControl()
    {
        const string markup = """
            <Border MotionClip="$Pulse">
              <Border.Resources>
                <MotionClip Name="Pulse" TargetType="Border">
                  @animate { @to { Opacity = 1; } }
                </MotionClip>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipDirectAssignment.crn", markup, out _);

        AssertMotionDiagnostic(result, "cannot be assigned directly");
    }

    [Fact]
    public void MotionRunIsRejectedOutsideAnAspect()
    {
        const string markup = """
            <Border>
              @run $Pulse;
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionRunOutsideAspect.crn", markup, out _);

        AssertMotionDiagnostic(result, "Aspect");
    }
}
