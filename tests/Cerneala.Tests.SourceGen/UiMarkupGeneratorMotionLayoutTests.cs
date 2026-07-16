using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void MotionLayoutGeneratesTypedIdentityAndOptionsBeforeAttachment()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionLayout.cui.xml",
            """
            <TextBlock Text="card">
              <TextBlock.Resources>
                <Aspect Target="TextBlock">
                  @layout id $self.Text with Tween(100ms, EaseOut);
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """,
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("LayoutMotionId = new global::Cerneala.UI.Motion.Layout.LayoutMotionId", generated);
        Assert.Contains("LayoutMotionOptions.Spring", generated);
        Assert.Contains("MotionSpec<global::Cerneala.UI.Media.Transform>", generated);
        Assert.Contains("@layout must be applied before the element is attached", generated);
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
            "InvalidMotionLayout.cui.xml",
            """
            <TextBlock Text="card">
              <TextBlock.Resources>
                <Aspect Target="TextBlock">
            """ + body + """
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """,
            out _);

        AssertMotionDiagnostic(result, expectedMessage);
    }
}
