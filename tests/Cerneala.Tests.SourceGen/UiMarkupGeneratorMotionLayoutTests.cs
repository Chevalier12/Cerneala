using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void MotionLayoutGeneratesTypedIdentityAndOptionsInTheAspectBehavior()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionLayout.crn",
            """
            <TextBlock Text="card">
              <TextBlock.Resources>
                <Aspect TargetType="TextBlock">
                  @layout id $self.Text with Tween(100ms, EaseOut);
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """,
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("ApplyAspectValue(target, global::Cerneala.UI.Elements.UIElement.LayoutMotionIdProperty, new global::Cerneala.UI.Motion.Layout.LayoutMotionId", generated);
        Assert.Contains("LayoutMotionOptions.Spring", generated);
        Assert.Contains("MotionSpec<global::Cerneala.UI.Media.Transform>", generated);
        Assert.DoesNotContain("must be applied before the element is attached", generated);
        Assert.DoesNotContain("CaptureFirstSnapshots", generated);
        Assert.DoesNotContain("StartCorrection", generated);
    }

    [Theory]
    [InlineData("@layout id $self.Text with Tween(100ms); @layout id $self.Text with Tween(100ms);", "only one")]
    [InlineData("@when IsEnabled { @layout id $self.Text with Tween(100ms); }", "directly inside")]
    [InlineData("@layout id $self.Text with Tween(100ms) mode position;", "modes")]
    [InlineData("@layout id $self.Text with Tween(100ms) crossfade;", "crossfade")]
    [InlineData("@layout id $self.Text with Tween(100ms) shared element;", "shared")]
    [InlineData("@layout id $self.Text with Sequence(Tween(100ms));", "Motion spec")]
    [InlineData("@layout id ($self.Text == \"card\") with Tween(100ms);", "one reactive source")]
    public void MotionLayoutRejectsUnsupportedOrRetroactiveShapes(string body, string expectedMessage)
    {
        GeneratorRunResult result = RunGenerator(
            "InvalidMotionLayout.crn",
            """
            <TextBlock Text="card">
              <TextBlock.Resources>
                <Aspect TargetType="TextBlock">
            """ + body + """
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """,
            out _);

        AssertMotionDiagnostic(result, expectedMessage);
    }

    [Theory]
    [InlineData("FastTween")]
    [InlineData("SharedTween")]
    [InlineData("CrossfadeTween")]
    [InlineData("ModeTween")]
    public void MotionLayoutAcceptsSpecResourceNamesThatContainReservedWords(string resourceName)
    {
        GeneratorRunResult result = RunGenerator(
            "MotionLayoutResourceName.crn",
            $$"""
            <TextBlock Text="card">
              <TextBlock.Resources>
                <Tween Name="{{resourceName}}" Duration="100ms" />
                <Aspect TargetType="TextBlock">
                  @layout id $self.Text with ${{resourceName}};
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """,
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
    }

    [Fact]
    public void MotionLayoutAcceptsAnIdPathThatContainsAReservedWord()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionLayoutIdPath.crn",
            """
            <TextBlock Text="card">
              <TextBlock.Resources>
                <Aspect TargetType="TextBlock">
                  @layout id $self.SharedText with Tween(100ms);
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """,
            out _);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI025");
    }
}
