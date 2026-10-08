using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;

namespace Cerneala.Tests.Controls;

// `$owner` of an Aspect program compiled once and applied at runtime: every
// element a component template creates knows its owner while the template
// instance is attached (docs/plans/2026-10-08-aspect-runtime-program.md).
public sealed class TemplateOwnershipTests
{
    [Fact]
    public void TemplateElementsKnowTheirOwnerAndLoseItWhenTheTemplateIsReplaced()
    {
        Button button = new();
        Border chrome = new();
        Border inner = new();
        chrome.Child = inner;
        button.ComponentTemplate = new ComponentTemplate<Button>("test", context => context.RequirePart("Chrome", chrome));

        Assert.Same(button, GeneratedMarkup.GetTemplateOwner(chrome));
        Assert.Same(button, GeneratedMarkup.GetTemplateOwner(inner));
        Assert.Same(chrome, GeneratedMarkup.GetTemplatePart<Border>(inner, "Chrome"));

        button.ComponentTemplate = new ComponentTemplate<Button>("other", _ => new Border());

        Assert.Throws<InvalidOperationException>(() => GeneratedMarkup.GetTemplateOwner(inner));
    }

    [Fact]
    public void NestedTemplateElementsKeepTheirOwnOwner()
    {
        Button inner = new();
        Border innerRoot = new();
        inner.ComponentTemplate = new ComponentTemplate<Button>("inner", _ => innerRoot);
        Button outer = new();
        outer.ComponentTemplate = new ComponentTemplate<Button>("outer", _ => inner);

        Assert.Same(outer, GeneratedMarkup.GetTemplateOwner(inner));
        Assert.Same(inner, GeneratedMarkup.GetTemplateOwner(innerRoot));
    }

    [Fact]
    public void MissingOrMistypedOwnerPartsThrow()
    {
        Button button = new();
        Border chrome = new();
        button.ComponentTemplate = new ComponentTemplate<Button>("test", context => context.RequirePart("Chrome", chrome));

        Assert.Throws<InvalidOperationException>(() => GeneratedMarkup.GetTemplatePart<TextBlock>(chrome, "Chrome"));
        Assert.Throws<InvalidOperationException>(() => GeneratedMarkup.GetTemplatePart<Border>(chrome, "Missing"));
        Assert.Throws<InvalidOperationException>(() => GeneratedMarkup.GetTemplateOwner(new Border()));
    }
}
