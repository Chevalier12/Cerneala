using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    // A control in another assembly (GeneratorTests has no internals access) gives a starting value in its
    // constructor. Markup, Aspect and C# values of the same property override it, and clearing them shows it again.
    [Fact]
    public void ExternalControlStartingValueIsOverriddenByMarkupAspectAndLocalValues()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            namespace TestInput;
            public sealed class MyCard : Border
            {
                public MyCard()
                {
                    SetFrameworkDefault(OpacityProperty, 0.9f);
                }
            }
            """;
        const string markup = """
            <StackPanel xmlns:local="clr-namespace:TestInput;assembly=GeneratorTests">
              <local:MyCard Opacity="0.5" />
              <local:MyCard>
                <local:MyCard.Aspect>
                  @default { Opacity = 0.3; }
                </local:MyCard.Aspect>
              </local:MyCard>
              <local:MyCard />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGeneratorWithInput("ControlDefaults.crn", markup, inputSource, out Compilation compilation);
        AssertNoGeneratorOrCompilationErrors(result, compilation);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.ControlDefaultsFactory"));
        UIElement markupCard = panel.VisualChildren[0];
        UIElement aspectCard = panel.VisualChildren[1];
        UIElement plainCard = panel.VisualChildren[2];
        AttachAndProcess(panel);

        Assert.Equal((0.5f, UiPropertyValueSource.MarkupBase), (markupCard.Opacity, markupCard.GetValueSource(UIElement.OpacityProperty)));
        Assert.Equal((0.3f, UiPropertyValueSource.AspectBase), (aspectCard.Opacity, aspectCard.GetValueSource(UIElement.OpacityProperty)));
        Assert.Equal((0.9f, UiPropertyValueSource.Default), (plainCard.Opacity, plainCard.GetValueSource(UIElement.OpacityProperty)));

        markupCard.ClearValue(UIElement.OpacityProperty, UiPropertyValueSource.MarkupBase);
        Assert.Equal((0.9f, UiPropertyValueSource.Default), (markupCard.Opacity, markupCard.GetValueSource(UIElement.OpacityProperty)));

        plainCard.Opacity = 0.2f;
        Assert.Equal((0.2f, UiPropertyValueSource.Local), (plainCard.Opacity, plainCard.GetValueSource(UIElement.OpacityProperty)));
        plainCard.ClearValue(UIElement.OpacityProperty);
        Assert.Equal((0.9f, UiPropertyValueSource.Default), (plainCard.Opacity, plainCard.GetValueSource(UIElement.OpacityProperty)));
    }
}
