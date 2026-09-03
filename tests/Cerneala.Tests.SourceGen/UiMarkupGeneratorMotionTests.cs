using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    private const string MotionBindingInputSource = """
        using System.ComponentModel;
        namespace TestInput;

        public sealed class MotionViewModel : INotifyPropertyChanged
        {
            private float targetOpacity = 0.4f;
            public event PropertyChangedEventHandler? PropertyChanged;
            public float TargetOpacity
            {
                get => targetOpacity;
                set
                {
                    targetOpacity = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TargetOpacity)));
                }
            }
        }

        public sealed class SnapshotMotionViewModel
        {
            public float TargetOpacity { get; set; } = 0.4f;
        }
        """;

    [Fact]
    public void MotionDirectReferenceIsAStartTimeSnapshot()
    {
        const string markup = """
            <Border DataType="TestInput.SnapshotMotionViewModel">
              <Border.Aspect>
                <Aspect>
                  @on Loaded
                  {
                    @animate with Tween(180ms, EaseOut)
                    {
                      @to { Opacity = $DataContext.TargetOpacity; }
                    }
                  }
                </Aspect>
              </Border.Aspect>
            </Border>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "MotionDirectReference.crn",
            markup,
            MotionBindingInputSource,
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("ReadReference<float>", generated, StringComparison.Ordinal);
        Assert.Contains("StartMotionProperty", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("StartBoundMotionProperty", generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("OneWay")]
    [InlineData("TwoWay")]
    public void MotionExplicitBindingTracksTheDestinationDuringTheAnimation(string mode)
    {
        string markup = $$"""
            <Border DataType="TestInput.MotionViewModel">
              <Border.Aspect>
                <Aspect>
                  @on Loaded
                  {
                    @animate with Tween(180ms, EaseOut)
                    {
                      @to { Opacity = $DataContext.TargetOpacity:{{mode}}; }
                    }
                  }
                </Aspect>
              </Border.Aspect>
            </Border>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "Motion" + mode + "Binding.crn",
            markup,
            MotionBindingInputSource,
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("StartBoundMotionProperty", generated, StringComparison.Ordinal);
        Assert.Contains("BindingMode." + mode, generated, StringComparison.Ordinal);
    }

    [Fact]
    public void MotionExplicitBindingIsRejectedOutsideTheAnimationDestination()
    {
        const string markup = """
            <Border DataType="TestInput.MotionViewModel">
              <Border.Aspect>
                <Aspect>
                  @on Loaded
                  {
                    @animate
                    {
                      @from { Opacity = $DataContext.TargetOpacity:OneWay; }
                      @to { Opacity = 1; }
                    }
                  }
                </Aspect>
              </Border.Aspect>
            </Border>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "MotionInvalidSourceBinding.crn",
            markup,
            MotionBindingInputSource,
            out _);

        AssertMotionDiagnostic(result, "allowed only as an @animate destination");
    }

    [Fact]
    public void PrismMotionExplicitBindingTracksTheDestinationDuringTheAnimation()
    {
        const string markup = """
            <Border DataType="TestInput.MotionViewModel">
              <Border.Aspect>
                <Aspect>
                  @on Loaded
                  {
                    @animate with Tween(180ms)
                    {
                      @to { $self.prism.Card.Opacity = $DataContext.TargetOpacity:OneWay; }
                    }
                  }
                </Aspect>
              </Border.Aspect>
              @prism { @layer Card { Opacity = 1; @filter Blur { Radius = 1; } } }
            </Border>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "PrismMotionBinding.crn",
            markup,
            MotionBindingInputSource,
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("StartBoundPrismMotionProperty", generated, StringComparison.Ordinal);
        Assert.Contains("BindingMode.OneWay", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void MotionMarkupNamedAspectGeneratesTypedTweenAndSpringResources()
    {
        const string markup = """
            <Border Aspect="$NavigationHover">
              <Border.Resources>
                <Tween Name="QuickOut" Duration="180ms" Easing="EaseOut" />
                <Spring Name="Responsive" Stiffness="520" Damping="38" Mass="1" />
                <Aspect Name="NavigationHover" TargetType="Border">
                  @when IsMouseOver
                  {
                    @animate with $QuickOut
                    {
                      @from { Opacity = current; Scale = current; }
                      @to { Opacity = 1; Scale = 1.04 with $Responsive; }
                    }
                  }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("NamedMotionAspect.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("NavigationHover", generated, StringComparison.Ordinal);
        Assert.Contains("Tween", generated, StringComparison.Ordinal);
        Assert.Contains("Spring", generated, StringComparison.Ordinal);
        Assert.Contains("OpacityProperty", generated, StringComparison.Ordinal);
        Assert.Contains("ScaleProperty", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void MotionMarkupCreatesIndependentSessionsForEachNamedAspectApplication()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <Aspect Name="Pulse" TargetType="Border">
                  @on Loaded { @animate { @to { Opacity = 0.5; } } }
                </Aspect>
              </StackPanel.Resources>
              <Border Aspect="$Pulse" />
              <Border Aspect="$Pulse" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("IndependentMotionSessions.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Equal(2, generated.Split("AttachMotionSession", StringSplitOptions.None).Length - 1);
        Assert.Equal(2, generated.Split(".Aspect!);", StringSplitOptions.None).Length - 1);
        Assert.Equal(2, generated.Split("AddMotionTrigger", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void MotionMarkupInlineAspectSupportsCurrentDefaultAndPerPropertySpecs()
    {
        const string markup = """
            <Border>
              <Border.Aspect>
                <Aspect>
                  @when IsMouseOver
                  {
                    @animate
                    {
                      @from { Opacity = current; Scale = current; }
                      @to { Opacity = 1; Scale = 1.04 with Tween(160ms, EaseOut); }
                    }
                  }
                </Aspect>
              </Border.Aspect>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("InlineMotionAspect.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("Tween", generated, StringComparison.Ordinal);
        Assert.Contains("OpacityProperty", generated, StringComparison.Ordinal);
        Assert.Contains("ScaleProperty", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void MotionMarkupEmitsAllSupportedPropertyStartOptions()
    {
        const string markup = """
            <Border Aspect="$NavigationHover">
              <Border.Resources>
                <Aspect Name="NavigationHover" TargetType="Border">
                  @when IsMouseOver
                  {
                    @animate with Tween(180ms, EaseOut)
                    {
                      retarget = PreserveProgress;
                      holdOnComplete = false;
                      debugName = "Tour/NavigationHover";
                      @from { Opacity = current; }
                      @to { Opacity = 1; }
                    }
                  }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionOptions.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("PreserveProgress", generated, StringComparison.Ordinal);
        Assert.Contains("HoldOnComplete = false", generated, StringComparison.Ordinal);
        Assert.Contains("Tour/NavigationHover", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void MotionMarkupRequiresToBlock()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionMissingTo.crn",
            MotionAspectMarkup(
                """
                @when IsMouseOver
                {
                  @animate with Tween(180ms, EaseOut)
                  {
                    @from { Opacity = current; }
                  }
                }
                """),
            out _);

        AssertMotionDiagnostic(result, "@to");
    }

    [Fact]
    public void MotionMarkupReportsUnknownTargetProperty()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionUnknownProperty.crn",
            MotionAspectMarkup(
                """
                @when IsMouseOver
                {
                  @animate with Tween(180ms, EaseOut)
                  {
                    @to { DoesNotExist = 1; }
                  }
                }
                """),
            out _);

        AssertMotionDiagnostic(result, "DoesNotExist");
    }

    [Fact]
    public void MotionMarkupReportsIncompatibleTargetPropertyValue()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionWrongValue.crn",
            MotionAspectMarkup(
                """
                @when IsMouseOver
                {
                  @animate with Tween(180ms, EaseOut)
                  {
                    @to { Opacity = "opaque"; }
                  }
                }
                """),
            out _);

        AssertMotionDiagnostic(result, "Opacity");
        AssertMotionDiagnostic(result, "compatible");
    }

    [Fact]
    public void MotionMarkupReportsMissingMixer()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionMissingMixer.crn",
            MotionAspectMarkup(
                """
                @when IsMouseOver
                {
                  @animate with Tween(180ms, EaseOut)
                  {
                    @to { Visibility = Visible; }
                  }
                }
                """),
            out _);

        AssertMotionDiagnostic(result, "Visibility");
        AssertMotionDiagnostic(result, "mixer");
    }

    [Fact]
    public void MotionMarkupRequiresEveryFromPropertyToAppearInTo()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionUnpairedFrom.crn",
            MotionAspectMarkup(
                """
                @when IsMouseOver
                {
                  @animate with Tween(180ms, EaseOut)
                  {
                    @from { Opacity = current; Scale = current; }
                    @to { Opacity = 1; }
                  }
                }
                """),
            out _);

        AssertMotionDiagnostic(result, "Scale");
        AssertMotionDiagnostic(result, "@to");
    }

    [Fact]
    public void MotionMarkupReportsUnknownMotionResource()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionUnknownResource.crn",
            MotionAspectMarkup(
                """
                @when IsMouseOver
                {
                  @animate with $DoesNotExist
                  {
                    @to { Opacity = 1; }
                  }
                }
                """),
            out _);

        AssertMotionDiagnostic(result, "$DoesNotExist");
    }

    [Theory]
    [InlineData("conflict = Replace;")]
    [InlineData("channel = Visual;")]
    [InlineData("reducedMotion = Skip;")]
    public void MotionMarkupRejectsUnsupportedStartOptions(string option)
    {
        GeneratorRunResult result = RunGenerator(
            "MotionUnsupportedOption.crn",
            MotionAspectMarkup($$"""
                @on Loaded
                {
                  @animate
                  {
                    {{option}}
                    @to { Opacity = 1; }
                  }
                }
                """),
            out _);

        AssertMotionDiagnostic(result, "option");
    }

    [Fact]
    public void MotionMarkupResolvesForwardNamedElementTargetsStatically()
    {
        const string markup = """
            <Border Aspect="$ForwardNamedMotion">
              <Border.Resources>
                <Aspect Name="ForwardNamedMotion" TargetType="Border">
                  @on Loaded
                  {
                    @animate { @to { $Child.Opacity = 1; } }
                  }
                </Aspect>
              </Border.Resources>
              <Border Name="Child" />
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionForwardNamedElement.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
    }

    [Fact]
    public void MotionMarkupResolvesSelfOwnerAndTheirTemplatePartsStatically()
    {
        const string markup = """
            <Grid Aspect="$HostMotion">
              <Grid.Resources>
                <Aspect Name="HostMotion" TargetType="Grid">
                  @on Loaded
                  {
                    @set
                    {
                      $Target.parts.$Chrome.Opacity = 0.7;
                    }
                  }
                </Aspect>
                <Aspect Name="TemplateChildMotion" TargetType="Border">
                  @on Loaded
                  {
                    @animate
                    {
                      @to
                      {
                        $owner.Opacity = 0.9;
                        $owner.parts.$Chrome.Opacity = 0.8;
                      }
                    }
                  }
                </Aspect>
                <Aspect Name="TemplatedMotion" TargetType="ToggleButton">
                  @on Loaded
                  {
                    @animate
                    {
                      @to
                      {
                        $self.Opacity = 1;
                        $self.parts.$Chrome.ScaleX = 1;
                      }
                    }
                  }
                  @template
                  {
                    <Border Name="Chrome" Aspect="$TemplateChildMotion" />
                  }
                </Aspect>
              </Grid.Resources>
              <ToggleButton Name="Target" Aspect="$TemplatedMotion" />
            </Grid>
            """;

        GeneratorRunResult result = RunGenerator("MotionSelfOwnerParts.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("ComponentTemplateInstance!.Parts[\"Chrome\"]", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Reflection", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void MotionMarkupRejectsPartShorthandForNamedElements()
    {
        const string markup = """
            <Border Aspect="$Motion">
              <Border.Resources>
                <Aspect Name="Motion" TargetType="Border">
                  @on Loaded { @animate { @to { $part.Child.Opacity = 1; } } }
                </Aspect>
              </Border.Resources>
              <Border Name="Child" />
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionPartShorthand.crn", markup, out _);

        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.GetMessage().Contains("$part", StringComparison.Ordinal));
    }

    [Fact]
    public void MotionMarkupAcceptsBaseTargetTypesByAssignability()
    {
        const string markup = """
            <Border Aspect="$BaseMotion">
              <Border.Resources>
                <Aspect Name="BaseMotion" TargetType="Control">
                  @on Loaded
                  {
                    @animate { @to { Opacity = 1; } }
                  }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionDerivedTarget.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
    }

    [Fact]
    public void MotionMarkupSpecializesSpecsWithoutRuntimeNameLookup()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionTypedSpec.crn",
            MotionAspectMarkup(
                """
                @on Loaded
                {
                  @animate with Tween(180ms, EaseOut)
                  {
                    @from { Opacity = current; }
                    @to { Opacity = 1; }
                  }
                }
                """),
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("MotionSpec<float>", generated, StringComparison.Ordinal);
        Assert.Contains("TweenSpec<float>", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProperty", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Reflection", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void MotionMarkupReportsWrongAspectTargetType()
    {
        const string markup = """
            <Button Aspect="$BorderOnly">
              <Button.Resources>
                <Aspect Name="BorderOnly" TargetType="Border">
                  @on Loaded
                  {
                    @animate { @to { Opacity = 1; } }
                  }
                </Aspect>
              </Button.Resources>
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("MotionWrongTarget.crn", markup, out _);

        AssertMotionDiagnostic(result, "Aspect");
    }

    [Fact]
    public void MotionMarkupRejectsDirectivesOutsideAnAspect()
    {
        const string markup = """
            <Border>
              @animate with Tween(180ms, EaseOut)
              {
                @to { Opacity = 1; }
              }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionIllegalContext.crn", markup, out _);

        AssertMotionDiagnostic(result, "@animate");
        AssertMotionDiagnostic(result, "Aspect");
    }

    [Fact]
    public void PublicMotionOptionsApiCanBeUsedByAnExternalGeneratedConsumer()
    {
        const string markup = "<Border />";
        const string inputSource = """
            using System;
            using Cerneala.UI.Elements;
            using Cerneala.UI.Motion;
            using Cerneala.UI.Motion.Properties;
            using Cerneala.UI.Motion.Specs;

            namespace TestInput;

            public static class MotionConsumer
            {
                public static void Start(UIElement element)
                {
                    MotionPropertyStartOptions options = new()
                    {
                        RetargetMode = RetargetMode.PreserveProgress,
                        HoldOnComplete = false,
                        DebugName = "Generated/Options"
                    };

                    element.Motion()
                        .Animate(UIElement.OpacityProperty)
                        .From(0f)
                        .To(1f)
                        .With(Motion.Tween<float>(TimeSpan.FromMilliseconds(180), Easings.EaseOut), options);
                }
            }
            """;

        GeneratorRunResult result = RunGeneratorWithInput("PublicMotionOptions.crn", markup, inputSource, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
    }

    [Fact]
    public void MotionGrammarParsesTypedValuesSpecsOptionsAndEventBodies()
    {
        string markup = MotionAspectMarkup(
            """
                  @on Loaded
                  {
                    @parallel
                    {
                      @animate with Tween(180ms, EaseOut)
                      {
                        retarget = PreserveProgress;
                        holdOnComplete = true;
                        debugName = "Grammar/Event";
                        @from { Opacity = current; }
                        @to
                        {
                          Opacity = IsMouseOver ? 1 : 0.72;
                          Scale = 1.04 with Spring(520, 38, 1);
                        }
                      }
                      @animate with Spring(520, 38, 1)
                      {
                        @to { Scale = 1; }
                      }
                    }
                  }
            """);

        GeneratorRunResult result = RunGenerator("MotionGrammar.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
    }

    [Theory]
    [InlineData("@animate { @to { Opacity = 1; }", "closing")]
    [InlineData("@animate { @to { Opacity = 1 } }", "end with")]
    [InlineData("@animate { debugName = \"unterminated; @to { Opacity = 1; } }", "quote")]
    [InlineData("@animate { @unknown { Opacity = 1; } @to { Opacity = 1; } }", "@unknown")]
    public void MotionGrammarRecoversWithFocusedDiagnostics(string execution, string expectedMessage)
    {
        GeneratorRunResult result = RunGenerator(
            "MotionRecovery.crn",
            MotionAspectMarkup($$"""
                @on Loaded
                {
                  {{execution}}
                }
                """),
            out _);

        AssertMotionDiagnostic(result, expectedMessage);
    }

    [Fact]
    public void MotionGrammarRejectsXmlControlsInsideExecutionBodies()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionXmlExecution.crn",
            MotionAspectMarkup(
                """
                @on Click
                {
                  @animate
                  {
                    <Border />
                    @to { Opacity = 1; }
                  }
                }
                """),
            out _);

        AssertMotionDiagnostic(result, "XML controls");
    }

    [Fact]
    public void MotionEventTriggerEmitsDirectBuiltInSubscription()
    {
        const string markup = """
            <Button>
              <Button.Aspect>
                <Aspect>
                  @on Click { @animate { @to { Opacity = 1; } } }
                </Aspect>
              </Button.Aspect>
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("MotionBuiltInEvent.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains(".Click += motionEventHandler", generated, StringComparison.Ordinal);
        Assert.Contains(".Click -= motionEventHandler", generated, StringComparison.Ordinal);
        Assert.Contains("AttachMotionSession", generated, StringComparison.Ordinal);
        Assert.Contains("AddMotionTrigger", generated, StringComparison.Ordinal);
        Assert.Contains("StartMotionProperty", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Reflection", generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Fired")]
    [InlineData("Inherited")]
    [InlineData("RoutedFired")]
    public void MotionEventTriggerSupportsCustomClrRoutedAndInheritedEvents(string eventName)
    {
        const string inputSource = """
            using System;
            using Cerneala.UI.Controls;
            using Cerneala.UI.Elements;
            using Cerneala.UI.Input;

            namespace TestInput.Views;

            public class EventBase : Border
            {
                public event EventHandler? Inherited;
            }

            public sealed class EventBorder : EventBase
            {
                public static readonly RoutedEvent RoutedFiredEvent = RoutedEventRegistry.Register(
                    nameof(RoutedFired), typeof(EventBorder), RoutingStrategy.Bubble, typeof(RoutedEventArgs));

                public event EventHandler? Fired;

                public event RoutedEventHandler RoutedFired
                {
                    add => AddHandler(RoutedFiredEvent, value);
                    remove => RemoveHandler(RoutedFiredEvent, value);
                }
            }

            public partial class MainWindow : Window { }
            """;
        string markup = $$"""
            <Window xmlns:views="clr-namespace:TestInput.Views;assembly=GeneratorTests">
              <Window.Resources>
                <Aspect Name="Motion" TargetType="views:EventBorder">
                  @on {{eventName}} { @animate { @to { Opacity = 1; } } }
                </Aspect>
              </Window.Resources>
              <views:EventBorder Aspect="$Motion" />
            </Window>
            """;

        GeneratorRunResult result = RunPairedGenerator("Views/MainWindow.crn", markup, inputSource, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("." + eventName + " +=", generated, StringComparison.Ordinal);
        Assert.Contains("." + eventName + " -=", generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Missing")]
    [InlineData("NotAnEvent")]
    [InlineData("PrivateEvent")]
    public void MotionEventTriggerReportsMissingOrNonEventMembers(string eventName)
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public sealed class EventBorder : Border
            {
                public int NotAnEvent { get; set; }
                private event System.EventHandler? PrivateEvent;
            }
            public partial class MainWindow : Window { }
            """;
        string markup = $$"""
            <Window xmlns:views="clr-namespace:TestInput.Views;assembly=GeneratorTests">
              <Window.Resources>
                <Aspect Name="Motion" TargetType="views:EventBorder">
                  @on {{eventName}} { @animate { @to { Opacity = 1; } } }
                </Aspect>
              </Window.Resources>
              <views:EventBorder Aspect="$Motion" />
            </Window>
            """;

        GeneratorRunResult result = RunPairedGenerator("Views/MainWindow.crn", markup, inputSource, out _);

        AssertMotionDiagnostic(result, eventName);
    }

    [Fact]
    public void MotionEventTriggerRejectsTargetTypeThatIsTooGeneral()
    {
        const string markup = """
            <Button Aspect="$Motion">
              <Button.Resources>
                <Aspect Name="Motion" TargetType="UIElement">
                  @on Click { @animate { @to { Opacity = 1; } } }
                </Aspect>
              </Button.Resources>
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("MotionGeneralEventTarget.crn", markup, out _);

        AssertMotionDiagnostic(result, "Click");
    }

    private static string MotionAspectMarkup(string body)
    {
        return $"""
            <Border>
              <Border.Resources>
                <Aspect TargetType="Border">
            {body}
                </Aspect>
              </Border.Resources>
            </Border>
            """;
    }

    private static void AssertNoGeneratorOrCompilationErrors(GeneratorRunResult result, Compilation compilation)
    {
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Diagnostic[] compilationErrors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        string generatedSources = string.Join(
            Environment.NewLine + "-----" + Environment.NewLine,
            result.GeneratedSources.Select(source => source.SourceText.ToString()));
        Assert.True(
            compilationErrors.Length == 0,
            string.Join(Environment.NewLine, compilationErrors.Select(diagnostic => diagnostic.ToString())) +
            Environment.NewLine + generatedSources);
    }

    private static void AssertMotionDiagnostic(GeneratorRunResult result, string expectedMessageFragment)
    {
        Diagnostic diagnostic = Assert.Single(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Contains(expectedMessageFragment, diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
    }
}
