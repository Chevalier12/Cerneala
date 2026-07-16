using System;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void MotionCompositionGeneratesParallelContainingSequence()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionParallelSequence.cui.xml",
            MotionAspectMarkup(
                """
                @on Loaded
                {
                  @parallel
                  {
                    @animate { @to { Opacity = 1; } }
                    @sequence
                    {
                      @animate { @to { Scale = 0.9; } }
                      @animate { @to { Scale = 1; } }
                    }
                  }
                }
                """),
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("MarkupMotionExecution.Parallel", generated, StringComparison.Ordinal);
        Assert.Contains("MarkupMotionExecution.Sequence", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void MotionCompositionGeneratesSequenceContainingParallel()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionSequenceParallel.cui.xml",
            MotionAspectMarkup(
                """
                @on Loaded
                {
                  @sequence
                  {
                    @animate { @to { Opacity = 0.5; } }
                    @parallel
                    {
                      @animate { @to { Opacity = 1; } }
                      @animate { @to { Scale = 1; } }
                    }
                  }
                }
                """),
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("MarkupMotionExecution.Sequence", generated, StringComparison.Ordinal);
        Assert.Contains("MarkupMotionExecution.Parallel", generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("@parallel { }", "at least one child")]
    [InlineData("@sequence { }", "at least one child")]
    public void MotionCompositionRejectsEmptyGroups(string execution, string expectedMessage)
    {
        GeneratorRunResult result = RunGenerator(
            "MotionEmptyGroup.cui.xml",
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
    public void MotionCompositionRequiresExplicitRelationshipBetweenSiblings()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionSiblingExecutions.cui.xml",
            MotionAspectMarkup(
                """
                @on Loaded
                {
                  @animate { @to { Opacity = 1; } }
                  @animate { @to { Scale = 1; } }
                }
                """),
            out _);

        AssertMotionDiagnostic(result, "@parallel or @sequence");
    }
}
