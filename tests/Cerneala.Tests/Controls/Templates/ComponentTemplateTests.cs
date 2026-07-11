using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls.Templates;

public sealed class ComponentTemplateTests
{
    [Fact]
    public void TypedComponentTemplateReceivesOwnerStateVariantsAndTokens()
    {
        AspectToken<Color> token = AspectToken.Color("button.background");
        AspectEnvironment environment = new("template");
        environment.Set(token, Color.White);
        Button button = new();
        Color observed = Color.Transparent;

        ComponentTemplate<Button> template = new("modern", context =>
        {
            observed = (Color)token.Ref().Resolve(new AspectResolutionContext(context.Owner, context.Environment, context.States, context.Variants))!;
            return new Border();
        });

        template.CreateInstance(button, new ComponentTemplateContext(button, environment, AspectStateSet.Empty.Add(AspectState.Hover), AspectVariantSet.Empty));

        Assert.Equal(Color.White, observed);
    }

    [Fact]
    public void TemplateRegistersNamedSlots()
    {
        AspectSlot<Button, Border> rootSlot = AspectSlot.For<Button, Border>("Root");
        Button button = new();
        Border border = new();
        ComponentTemplate<Button> template = new("modern", context =>
        {
            context.RegisterSlot(rootSlot, border);
            return border;
        });

        ComponentTemplateInstance instance = template.CreateInstance(button, new ComponentTemplateContext(button, new AspectEnvironment("template")));

        Assert.Same(border, instance.Slots[rootSlot]);
    }

    [Fact]
    public void TemplateRegistersRequiredParts()
    {
        Button button = new();
        ContentPresenter presenter = new();
        ComponentTemplate<Button> template = new("modern", context =>
        {
            context.RequirePart("PART_Content", presenter);
            return presenter;
        });

        ComponentTemplateInstance instance = template.CreateInstance(button, new ComponentTemplateContext(button, new AspectEnvironment("template")));

        Assert.Same(presenter, instance.Parts["PART_Content"]);
    }

    [Fact]
    public void MissingRequiredPartFailsWithClearDiagnostic()
    {
        Button button = new();
        ComponentTemplate<Button> template = new("modern", context =>
        {
            context.RequirePart<ContentPresenter>("PART_Content", null);
            return null;
        });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            template.CreateInstance(button, new ComponentTemplateContext(button, new AspectEnvironment("template"))));

        Assert.Contains("PART_Content", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ChangingTemplateDetachesOldInstanceAndBindings()
    {
        Button button = new();
        Border oldRoot = new();
        Border newRoot = new();
        button.ComponentTemplate = new ComponentTemplate<Button>("old", _ => oldRoot);

        button.ComponentTemplate = new ComponentTemplate<Button>("new", _ => newRoot);

        Assert.Null(oldRoot.LogicalParent);
        Assert.Same(button, newRoot.LogicalParent);
    }

    [Fact]
    public void SameTemplateKeepsStableGeneratedRoot()
    {
        Button button = new();
        int created = 0;
        ComponentTemplate<Button> template = new("modern", _ =>
        {
            created++;
            return new Border();
        });
        button.ComponentTemplate = template;
        Border root = Assert.IsType<Border>(button.ComponentTemplateInstance!.Root);

        button.ApplyTemplate();

        Assert.Equal(1, created);
        Assert.Same(root, button.ComponentTemplateInstance!.Root);
    }

    [Fact]
    public void TokenBindingUpdatesTargetFromEnvironment()
    {
        AspectToken<Thickness> token = AspectToken.Thickness("button.padding");
        AspectEnvironment environment = new("template");
        environment.Set(token, new Thickness(8));
        Button button = new();
        Border border = new();
        ComponentTemplate<Button> template = new("modern", context =>
        {
            context.BindToken(token, border, Control.PaddingProperty);
            return border;
        });

        ComponentTemplateInstance instance = template.CreateInstance(button, new ComponentTemplateContext(button, environment));
        instance.Attach(button);

        Assert.Equal(new Thickness(8), border.Padding);
    }
}
