namespace Cerneala.UI.Animation;

public sealed class Storyboard
{
    private readonly List<AnimationScheduler.AnimationHandle> handles = new();

    public IReadOnlyList<AnimationScheduler.AnimationHandle> Handles => handles.AsReadOnly();

    public void Add(AnimationScheduler.AnimationHandle handle)
    {
        handles.Add(handle ?? throw new ArgumentNullException(nameof(handle)));
    }

    public void Stop()
    {
        foreach (AnimationScheduler.AnimationHandle handle in handles)
        {
            handle.Stop();
        }
    }
}
