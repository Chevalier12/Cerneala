using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Buttons;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectTemplateCatalogIntegrationTests
{
    [Fact]
    public void ControlResolvesNamedComponentTemplateFromRootCatalog()
    {
        Border generated = new();
        ComponentTemplate<Button> template = new("App.Button", _ => generated);
        Button button = new() { ComponentTemplateKey = "App.Button" };
        UIRoot root = RootWith(button);
        root.AspectRegistry.Register(AspectPackage.Create("App")
            .Components(components => components.AddTemplate(
                new ComponentTemplateDefinition("App.Button", typeof(Button), template))));

        root.AspectProcessor.Process(button);

        Assert.Same(generated, button.ComponentTemplateInstance!.Root);
    }

    [Fact]
    public void LaterPackageReplacesNamedComponentTemplateWithSameOwnerSpecificity()
    {
        Border firstRoot = new();
        Border secondRoot = new();
        ComponentTemplate<Button> first = new("First", _ => firstRoot);
        ComponentTemplate<Button> second = new("Second", _ => secondRoot);
        Button button = new() { ComponentTemplateKey = "App.Button" };
        UIRoot root = RootWith(button);
        root.AspectRegistry.Register(AspectPackage.Create("First")
            .Components(components => components.AddTemplate(
                new ComponentTemplateDefinition("App.Button", typeof(Button), first))));
        root.AspectProcessor.Process(button);

        root.AspectRegistry.Register(AspectPackage.Create("Second")
            .Components(components => components.AddTemplate(
                new ComponentTemplateDefinition("App.Button", typeof(Button), second))));
        root.AspectProcessor.Process(button);

        Assert.Same(secondRoot, button.ComponentTemplateInstance!.Root);
        Assert.Null(firstRoot.LogicalParent);
    }

    [Fact]
    public void ContentPresenterFallsBackToContentTemplatesFromRootCatalog()
    {
        ContentTemplate<string> template = new(
            "App.String",
            key: "studio",
            priority: 0,
            _ => new Border());
        ContentPresenter presenter = new()
        {
            Content = "Aspect",
            ContentTemplateKey = "studio"
        };
        UIRoot root = RootWith(presenter);
        root.AspectRegistry.Register(AspectPackage.Create("Content")
            .Content(content => content.Add(
                new ContentTemplateDefinition("App.String", typeof(string), "studio", template))));

        root.AspectProcessor.Process(presenter);

        Assert.IsType<Border>(presenter.PresentedChild);
    }

    [Fact]
    public void ContentTemplateInsideComponentReceivesTemplateOwnerVariants()
    {
        AspectSlot<Button, ContentPresenter> slot = AspectSlot.For<Button, ContentPresenter>("Content");
        AspectVariantSet? observedVariants = null;
        object? observedOwner = null;
        ContentTemplate<string> contentTemplate = new(
            "App.String",
            key: null,
            priority: 0,
            context =>
            {
                observedVariants = context.Variants;
                observedOwner = context.Owner;
                return new Border();
            });
        ComponentTemplate<Button> componentTemplate = new("App.Button", context =>
        {
            ContentPresenter presenter = new();
            context.RegisterSlot(slot, presenter);
            context.Bind(ContentControl.ContentProperty, presenter, ContentPresenter.ContentProperty);
            return presenter;
        });
        Button button = new()
        {
            Content = "Aspect",
            ComponentTemplateKey = "App.Button"
        };
        button.SetAspectVariant(ButtonVariants.Kind, ButtonKind.Primary);
        UIRoot root = RootWith(button);
        root.AspectRegistry.Register(AspectPackage.Create("App")
            .Components(components => components.AddTemplate(
                new ComponentTemplateDefinition("App.Button", typeof(Button), componentTemplate)))
            .Content(content => content.Add(
                new ContentTemplateDefinition("App.String", typeof(string), key: null, contentTemplate))));

        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Same(button, observedOwner);
        Assert.NotNull(observedVariants);
        Assert.True(observedVariants!.TryGet(ButtonVariants.Kind, out ButtonKind kind));
        Assert.Equal(ButtonKind.Primary, kind);
    }

    [Fact]
    public void SlotRuleUsesTemplateOwnerVariantsAndTargetsRegisteredElement()
    {
        AspectSlot<Button, Border> slot = AspectSlot.For<Button, Border>("Chrome");
        SolidColorBrush accent = new(new Color(77, 240, 255));
        Border chrome = new();
        ComponentTemplate<Button> template = new("App.Button", context =>
        {
            context.RegisterSlot(slot, chrome);
            return chrome;
        });
        AspectRuleSet slotRule = new AspectRuleSetBuilder(
            "button.primary.chrome",
            AspectLayer.App,
            new AspectTarget(
                typeof(Border),
                slot,
                [
                    AspectCondition.Variant(ButtonVariants.Kind, ButtonKind.Primary),
                    AspectCondition.State(AspectState.Hover)
                ]),
            declarationOrder: 0)
            .Set(Control.BackgroundProperty, AspectValue<Brush?>.Literal(accent), "chrome.background")
            .Build();
        Button button = new() { ComponentTemplateKey = "App.Button" };
        button.SetAspectVariant(ButtonVariants.Kind, ButtonKind.Primary);
        button.IsPointerOver = true;
        UIRoot root = RootWith(button);
        root.AspectRegistry.Register(AspectPackage.Create("App")
            .Components(components =>
            {
                components.AddTemplate(new ComponentTemplateDefinition("App.Button", typeof(Button), template));
                components.AddRule(slotRule);
            }));

        root.AspectProcessor.Process(button);
        root.AspectProcessor.Process(chrome);

        Assert.Same(accent, chrome.Background);
        AspectDiagnostics.Snapshot diagnostics = root.Detective.CaptureAspect(chrome);
        Assert.Equal(slot, diagnostics.ResolvedAspect!.Dependencies.Slot);
        Assert.Contains(ButtonVariants.Kind, diagnostics.ResolvedAspect.Dependencies.Variants);
        Assert.Contains(AspectState.Hover, diagnostics.ResolvedAspect.Dependencies.States);
    }

    [Fact]
    public void ReplacingTemplateRemovesSlotContextFromDetachedElements()
    {
        AspectSlot<Button, Border> slot = AspectSlot.For<Button, Border>("Chrome");
        SolidColorBrush accent = new(new Color(77, 240, 255));
        Border oldChrome = new();
        ComponentTemplate<Button> first = new("First", context =>
        {
            context.RegisterSlot(slot, oldChrome);
            return oldChrome;
        });
        ComponentTemplate<Button> second = new("Second", _ => new Border());
        AspectRuleSet slotRule = new AspectRuleSetBuilder(
            "button.chrome",
            AspectLayer.App,
            new AspectTarget(typeof(Border), slot),
            declarationOrder: 0)
            .Set(Control.BackgroundProperty, AspectValue<Brush?>.Literal(accent))
            .Build();
        Button button = new() { ComponentTemplate = first };
        UIRoot root = RootWith(button);
        root.AspectRegistry.Register(AspectPackage.Create("App")
            .Components(components => components.AddRule(slotRule)));
        root.AspectProcessor.Process(oldChrome);
        Assert.Same(accent, oldChrome.Background);

        button.ComponentTemplate = second;
        root.AspectProcessor.Process(oldChrome);

        Assert.Null(oldChrome.Background);
        Assert.Null(oldChrome.LogicalParent);
    }

    private static UIRoot RootWith(UIElement child)
    {
        UIRoot root = new();
        root.LogicalChildren.Add(child);
        root.VisualChildren.Add(child);
        return root;
    }
}
