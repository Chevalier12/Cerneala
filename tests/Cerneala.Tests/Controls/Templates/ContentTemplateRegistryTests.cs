using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls.Templates;

public sealed class ContentTemplateRegistryTests
{
    [Fact]
    public void RegistryResolvesTemplateByExactDataType()
    {
        ContentTemplate<UserCard> template = Template<UserCard>("user");
        ContentTemplateRegistry registry = new();
        registry.Register(template);

        Assert.True(registry.TryResolve(new ContentTemplateMatchContext(new UserCard("Ada")), out ContentTemplate? resolved));
        Assert.Same(template, resolved);
    }

    [Fact]
    public void RegistryResolvesTemplateByAssignableDataTypeWithNearestTypeWinning()
    {
        ContentTemplate<object> objectTemplate = Template<object>("object");
        ContentTemplate<Person> personTemplate = Template<Person>("person");
        ContentTemplateRegistry registry = new();
        registry.Register(objectTemplate);
        registry.Register(personTemplate);

        registry.TryResolve(new ContentTemplateMatchContext(new UserCard("Ada")), out ContentTemplate? resolved);

        Assert.Same(personTemplate, resolved);
    }

    [Fact]
    public void KeyedTemplateWinsWhenKeyRequested()
    {
        ContentTemplate<UserCard> normal = Template<UserCard>("normal");
        ContentTemplate<UserCard> keyed = Template<UserCard>("keyed", key: "compact");
        ContentTemplateRegistry registry = new();
        registry.Register(normal);
        registry.Register(keyed);

        registry.TryResolve(new ContentTemplateMatchContext(new UserCard("Ada"), requestedKey: "compact"), out ContentTemplate? resolved);

        Assert.Same(keyed, resolved);
    }

    [Fact]
    public void PredicateTemplateCanOverrideTypeTemplateByPriority()
    {
        ContentTemplate<UserCard> normal = Template<UserCard>("normal");
        ContentTemplate<UserCard> important = Template<UserCard>(
            "important",
            priority: 10,
            predicate: context => context.Data is UserCard { Important: true });
        ContentTemplateRegistry registry = new();
        registry.Register(normal);
        registry.Register(important);

        registry.TryResolve(new ContentTemplateMatchContext(new UserCard("Ada", Important: true)), out ContentTemplate? resolved);

        Assert.Same(important, resolved);
    }

    [Fact]
    public void MissingTemplateFallsBackToStringTextBlock()
    {
        ContentPresenter presenter = new() { Content = "hello", LocalTemplateRegistry = new ContentTemplateRegistry() };

        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.IsType<TextBlock>(presenter.PresentedChild);
    }

    [Fact]
    public void AssigningLocalTemplateRegistryRefreshesPreviouslyUnresolvedContent()
    {
        ContentPresenter presenter = new() { Content = new UserCard("Ada"), ContentTemplateKey = "card" };
        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));
        Assert.Null(presenter.PresentedChild);

        ContentTemplateRegistry registry = new();
        registry.Register(Template<UserCard>("card", key: "card"));
        presenter.LocalTemplateRegistry = registry;
        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.IsType<FixedElement>(presenter.PresentedChild);
    }

    [Fact]
    public void NullContentProducesNoChildUnlessNullTemplateRegistered()
    {
        ContentPresenter presenter = new() { Content = null, LocalTemplateRegistry = new ContentTemplateRegistry() };
        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));
        Assert.Null(presenter.PresentedChild);

        ContentTemplateRegistry registry = new();
        registry.Register(new ContentTemplate("null", dataType: null, key: null, priority: 0, factory: _ => new FixedElement()));
        presenter.LocalTemplateRegistry = registry;
        presenter.Content = "dirty";
        presenter.Content = null;
        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.IsType<FixedElement>(presenter.PresentedChild);
    }

    private static ContentTemplate<T> Template<T>(
        string name,
        string? key = null,
        int priority = 0,
        Func<ContentTemplateMatchContext, bool>? predicate = null)
    {
        return new ContentTemplate<T>(name, key, priority, _ => new FixedElement(), predicate);
    }

    private record Person(string Name);

    private sealed record UserCard(string Name, bool Important = false) : Person(Name);

    private sealed class FixedElement : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context) => new(1, 1);
    }
}
