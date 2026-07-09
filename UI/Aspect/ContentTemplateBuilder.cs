using Cerneala.UI.Controls.Templates;

namespace Cerneala.UI.Aspect;

public sealed class ContentTemplateBuilder
{
    private readonly List<ContentTemplateDefinition> templates;

    internal ContentTemplateBuilder(List<ContentTemplateDefinition> templates)
    {
        this.templates = templates;
    }

    public ContentTemplateBuilder Add(ContentTemplateDefinition template)
    {
        templates.Add(template ?? throw new ArgumentNullException(nameof(template)));
        return this;
    }
}
