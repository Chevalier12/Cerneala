namespace Cerneala.UI.Invalidation;

public interface IInvalidationSink
{
    void Invalidate(InvalidationRequest request);
}
