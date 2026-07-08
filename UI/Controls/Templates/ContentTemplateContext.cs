using Cerneala.UI.Aspect;

namespace Cerneala.UI.Controls.Templates;

public class ContentTemplateContext
{
    public ContentTemplateContext(
        object? data,
        ContentPresenter? presenter = null,
        AspectEnvironment? environment = null,
        AspectVariantSet? variants = null,
        int index = -1,
        object? owner = null)
    {
        Data = data;
        Presenter = presenter;
        Environment = environment ?? new AspectEnvironment("content-template");
        Variants = variants ?? AspectVariantSet.Empty;
        Index = index;
        Owner = owner;
    }

    public object? Data { get; }

    public ContentPresenter? Presenter { get; }

    public AspectEnvironment Environment { get; }

    public AspectVariantSet Variants { get; }

    public int Index { get; }

    public object? Owner { get; }
}

public sealed class ContentTemplateContext<TData> : ContentTemplateContext
{
    public ContentTemplateContext(ContentTemplateContext context)
        : base(context.Data, context.Presenter, context.Environment, context.Variants, context.Index, context.Owner)
    {
        Data = context.Data is TData typed ? typed : default;
    }

    public new TData? Data { get; }
}
