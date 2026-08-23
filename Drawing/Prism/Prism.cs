namespace Cerneala.Drawing.Prism;

public static class Prism
{
    public static PrismImage Apply(
        IDrawImage source,
        params PrismOperation[] operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        return Apply(source, new PrismPipeline(operations));
    }

    public static PrismImage Apply(
        IDrawImage source,
        PrismPipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(pipeline);
        return new PrismImage(source, pipeline);
    }
}
