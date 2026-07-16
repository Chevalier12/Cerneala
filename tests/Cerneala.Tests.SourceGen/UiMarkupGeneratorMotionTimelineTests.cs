using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void MotionKeyframesGeneratePerPropertySpecsWithSyntheticGapFrames()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionKeyframeGaps.cui.xml",
            MotionAspectMarkup(
                """
                @on Loaded
                {
                  @keyframes duration 1000ms
                  {
                    @animate 20%..40% with EaseOut { @from { Opacity = 0; } @to { Opacity = 1; } }
                    @animate 70%..100% with EaseIn { @from { Opacity = 0.5; } @to { Opacity = 0; } }
                  }
                }
                """),
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("KeyframesSpec<float>", generated);
        Assert.Contains("TimeSpan.FromMilliseconds(1000)", generated);
        Assert.True(Count(generated, "0.7f") >= 2, "The gap boundary must retain the previous value before the next segment starts.");
    }

    [Fact]
    public void MotionKeyframesComposeInsideMotionClipAndSequence()
    {
        const string markup = """
            <Border>
              <Border.Resources>
                <MotionClip Name="Pulse" TargetType="Border">
                  @keyframes duration 300ms
                  {
                    @animate 0%..100% { @from { Opacity = 0; } @to { Opacity = 1; } }
                  }
                </MotionClip>
                <Aspect TargetType="Border">
                  @on Loaded
                  {
                    @sequence
                    {
                      @run $Pulse;
                      @animate { @to { Scale = 1; } }
                    }
                  }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionKeyframeClip.cui.xml", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("KeyframesSpec<float>", generated);
        Assert.Contains("MarkupMotionExecution.Sequence", generated);
    }

    [Fact]
    public void MotionKeyframesGenerateStepEasing()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionKeyframeSteps.cui.xml",
            MotionAspectMarkup(
                """
                @on Loaded
                {
                  @keyframes duration 400ms
                  {
                    @animate 0%..100% with Step(4, JumpEnd)
                    {
                      @from { Opacity = 0; }
                      @to { Opacity = 1; }
                    }
                  }
                }
                """),
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        Assert.Contains("new global::Cerneala.UI.Motion.Specs.StepEasing(4", SingleGeneratedSource(result));
    }

    [Theory]
    [InlineData("Step(0, JumpEnd)")]
    [InlineData("Step(1, JumpNone)")]
    [InlineData("Step(4, Sideways)")]
    public void MotionKeyframesRejectInvalidStepEasing(string easing)
    {
        GeneratorRunResult result = RunGenerator(
            "MotionInvalidKeyframeStep.cui.xml",
            MotionAspectMarkup($$"""
                @on Loaded
                {
                  @keyframes duration 400ms
                  {
                    @animate 0%..100% with {{easing}} { @from { Opacity = 0; } @to { Opacity = 1; } }
                  }
                }
                """),
            out _);

        AssertMotionDiagnostic(result, "Step");
    }

    [Fact]
    public void MotionKeyframesMapHoldToKeyframeWithoutChangingCompletionPersistence()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionKeyframeHold.cui.xml",
            MotionAspectMarkup(
                """
                @on Loaded
                {
                  @keyframes duration 400ms
                  {
                    @animate 0%..100% hold { @from { Opacity = 0; } @to { Opacity = 1; } }
                  }
                }
                """),
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("Easings.Linear, true)", generated);
        Assert.Contains("HoldOnComplete = true", generated);
    }

    [Theory]
    [InlineData("@animate hold { @to { Opacity = 1; } }")]
    [InlineData("@animate with Step(4, JumpEnd) { @to { Opacity = 1; } }")]
    public void MotionHoldAndStepAreRejectedOutsideKeyframes(string animation)
    {
        GeneratorRunResult result = RunGenerator(
            "MotionHoldOutsideKeyframes.cui.xml",
            MotionAspectMarkup("@on Loaded { " + animation + " }"),
            out _);

        AssertMotionDiagnostic(result, "only inside @keyframes");
    }

    [Theory]
    [InlineData("50%..50%", "non-empty")]
    [InlineData("75%..25%", "ordered")]
    [InlineData("-10%..50%", "0%..100%")]
    [InlineData("50%..110%", "0%..100%")]
    public void MotionKeyframesRejectInvalidRanges(string range, string expectedMessage)
    {
        GeneratorRunResult result = RunGenerator(
            "MotionInvalidKeyframeRange.cui.xml",
            MotionAspectMarkup($$"""
                @on Loaded
                {
                  @keyframes duration 400ms
                  {
                    @animate {{range}} with Linear
                    {
                      @from { Opacity = 0; }
                      @to { Opacity = 1; }
                    }
                  }
                }
                """),
            out _);

        AssertMotionDiagnostic(result, expectedMessage);
    }

    [Fact]
    public void MotionKeyframesRejectOverlappingRangesForSameProperty()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionOverlappingKeyframes.cui.xml",
            MotionAspectMarkup(
                """
                @on Loaded
                {
                  @keyframes duration 400ms
                  {
                    @animate 0%..60% { @from { Opacity = 0; } @to { Opacity = 1; } }
                    @animate 50%..100% { @from { Opacity = 1; } @to { Opacity = 0; } }
                  }
                }
                """),
            out _);

        AssertMotionDiagnostic(result, "overlap");
    }

    [Fact]
    public void MotionKeyframesAllowOverlappingRangesForDifferentProperties()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionIndependentKeyframes.cui.xml",
            MotionAspectMarkup(
                """
                @on Loaded
                {
                  @keyframes duration 400ms
                  {
                    @animate 0%..70% { @from { Opacity = 0; } @to { Opacity = 1; } }
                    @animate 30%..100% { @from { Scale = 0.8; } @to { Scale = 1; } }
                  }
                }
                """),
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
    }

    [Theory]
    [InlineData("Spring(520, 38, 1)", "Spring")]
    [InlineData("Decay(1200)", "Decay")]
    [InlineData("Repeat(Tween(100ms, Linear), 2)", "Repeat")]
    [InlineData("PingPong(Tween(100ms, Linear), 2)", "PingPong")]
    public void MotionKeyframesRejectNonDeterministicOrWrappedSpecs(string spec, string expectedMessage)
    {
        GeneratorRunResult result = RunGenerator(
            "MotionInvalidKeyframeSpec.cui.xml",
            MotionAspectMarkup($$"""
                @on Loaded
                {
                  @keyframes duration 400ms
                  {
                    @animate 0%..100% with {{spec}}
                    {
                      @from { Opacity = 0; }
                      @to { Opacity = 1; }
                    }
                  }
                }
                """),
            out _);

        AssertMotionDiagnostic(result, expectedMessage);
    }

    [Theory]
    [InlineData("@sequence { @animate 0%..100% { @to { Opacity = 1; } } }", "ranged @animate")]
    [InlineData("@parallel { @animate 0%..100% { @to { Opacity = 1; } } }", "ranged @animate")]
    [InlineData("@keyframes duration 100ms { @animate 0%..100% { @to { Opacity = 1; } } }", "nested")]
    public void MotionKeyframesRejectNestedGroups(string child, string expectedMessage)
    {
        GeneratorRunResult result = RunGenerator(
            "MotionNestedKeyframes.cui.xml",
            MotionAspectMarkup($$"""
                @on Loaded
                {
                  @keyframes duration 400ms { {{child}} }
                }
                """),
            out _);

        AssertMotionDiagnostic(result, expectedMessage);
    }

    [Theory]
    [InlineData("Repeat(Spring(520, 38, 1), 2)", "Tween")]
    [InlineData("PingPong(Decay(1200), 2)", "Tween")]
    public void MotionWrappersRequireTween(string spec, string expectedMessage)
    {
        GeneratorRunResult result = RunGenerator(
            "MotionInvalidWrapper.cui.xml",
            MotionAspectMarkup($$"""
                @on Loaded
                {
                  @animate with {{spec}} { @to { Opacity = 1; } }
                }
                """),
            out _);

        AssertMotionDiagnostic(result, expectedMessage);
    }

    [Theory]
    [InlineData("Repeat(Tween(100ms, Linear), 3)", "RepeatSpec<float>")]
    [InlineData("Repeat(Tween(100ms, Linear), forever)", "RepeatSpec<float>")]
    [InlineData("PingPong(Tween(100ms, Linear), 4)", "PingPongSpec<float>")]
    public void MotionWrappersGenerateTypedTweenSpecs(string spec, string expectedCode)
    {
        GeneratorRunResult result = RunGenerator(
            "MotionWrapper.cui.xml",
            MotionAspectMarkup("@on Loaded { @animate with " + spec + " { @from { Opacity = 0; } @to { Opacity = 1; } } }"),
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains(expectedCode, generated);
        Assert.Contains("TweenSpec<float>", generated);
    }

    [Theory]
    [InlineData("Repeat(Tween(100ms), 0)")]
    [InlineData("PingPong(Tween(100ms), forever)")]
    public void MotionWrappersRejectInvalidCounts(string spec)
    {
        GeneratorRunResult result = RunGenerator(
            "MotionWrapperCount.cui.xml",
            MotionAspectMarkup("@on Loaded { @animate with " + spec + " { @to { Opacity = 1; } } }"),
            out _);

        AssertMotionDiagnostic(result, "positive");
    }

    [Fact]
    public void MotionStaggerSnapshotsVisualChildrenAndOffsetsTweenDelay()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <Aspect Target="StackPanel">
                  @on Loaded
                  {
                    @stagger target $part.Items each 40ms
                    {
                      @animate with Tween(100ms, Linear)
                      {
                        @from { Opacity = 0; }
                        @to { Opacity = 1; }
                      }
                    }
                  }
                </Aspect>
              </StackPanel.Resources>
              <StackPanel Name="Items">
                <Border />
                <Border />
              </StackPanel>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("MotionStagger.cui.xml", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("new global::System.Collections.Generic.List<global::Cerneala.UI.Elements.UIElement>(Items.VisualChildren)", generated);
        Assert.Contains("new global::Cerneala.UI.Motion.Core.MotionStagger(global::System.TimeSpan.FromMilliseconds(40))", generated);
        Assert.Contains(".GetDelay(", generated);
        Assert.Contains(".WithDelay(", generated);
        Assert.Contains("MarkupMotionExecution.Parallel(motionStaggerFactories", generated);
        Assert.Contains("StartMotionExecution", generated);
    }

    [Fact]
    public void MotionStaggerRejectsUnavailableCollectionPart()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionMissingStaggerPart.cui.xml",
            MotionAspectMarkup(
                "@on Loaded { @stagger target $part.Missing each 40ms { @animate with Tween(100ms) { @to { Opacity = 1; } } } }"),
            out _);

        AssertMotionDiagnostic(result, "not available");
    }

    [Fact]
    public void MotionResourcesGenerateAllTweenAndSpringOptions()
    {
        const string markup = """
            <Border>
              <Border.Resources>
                <Tween Name="Delayed" Duration="240ms" Delay="80ms" Easing="EaseOut" FillMode="Backwards" />
                <Spring Name="Responsive" Stiffness="520" Damping="38" Mass="1" RestSpeed="0.02" RestDelta="0.03" VelocityMode="Reset" />
                <Aspect Target="Border">
                  @on Loaded
                  {
                    @parallel
                    {
                      @animate with $Delayed { @to { Opacity = 1; } }
                      @animate with $Responsive { @to { Scale = 1; } }
                    }
                  }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionAdvancedSpecs.cui.xml", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("TimeSpan.FromMilliseconds(80)", generated);
        Assert.Contains("FillMode.Backwards", generated);
        Assert.Contains("0.02f, 0.03f", generated);
        Assert.Contains("SpringVelocityMode.Reset", generated);
    }

    [Theory]
    [InlineData("<Tween Name=\"Invalid\" Duration=\"100ms\" Delay=\"-1ms\" />", "Delay")]
    [InlineData("<Tween Name=\"Invalid\" Duration=\"100ms\" FillMode=\"Sticky\" />", "FillMode")]
    [InlineData("<Spring Name=\"Invalid\" RestSpeed=\"-1\" />", "RestSpeed")]
    [InlineData("<Spring Name=\"Invalid\" RestDelta=\"NaN\" />", "RestDelta")]
    [InlineData("<Spring Name=\"Invalid\" VelocityMode=\"Inherit\" />", "VelocityMode")]
    public void MotionResourcesRejectInvalidAdvancedOptions(string resource, string expectedMessage)
    {
        GeneratorRunResult result = RunGenerator(
            "MotionInvalidAdvancedSpec.cui.xml",
            AdvancedMotionResourceMarkup(resource),
            out _);

        AssertMotionDiagnostic(result, expectedMessage);
    }

    [Fact]
    public void MotionDecayResourceRemainsExplicitlyUnsupported()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionDeferredDecay.cui.xml",
            AdvancedMotionResourceMarkup("<Decay Name=\"Invalid\" ValueType=\"float\" InitialVelocity=\"1200\" Deceleration=\"0.998\" />"),
            out _);

        AssertMotionDiagnostic(result, "Decay");
    }

    [Theory]
    [InlineData("@animate with Spring(520, 38, 1) { @to { Opacity = 1; } }", "Tween")]
    [InlineData("@sequence { @animate { @to { Opacity = 1; } } }", "exactly one")]
    [InlineData("@animate { @to { Opacity = 1; } } @animate { @to { Scale = 1; } }", "exactly one")]
    public void MotionStaggerRequiresExactlyOneTweenAnimation(string body, string expectedMessage)
    {
        GeneratorRunResult result = RunGenerator(
            "MotionInvalidStagger.cui.xml",
            MotionAspectMarkup($$"""
                @on Loaded
                {
                  @stagger target $part.Items each 40ms
                  {
                    {{body}}
                  }
                }
                """),
            out _);

        AssertMotionDiagnostic(result, expectedMessage);
    }

    private static string AdvancedMotionResourceMarkup(string resource)
    {
        return $$"""
            <Border>
              <Border.Resources>
                {{resource}}
              </Border.Resources>
            </Border>
            """;
    }
}
