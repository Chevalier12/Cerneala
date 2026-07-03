using Cerneala.UI.Core;

namespace Cerneala.UI.Animation;

public sealed class AnimationScheduler
{
    private readonly List<IAnimationEntry> entries = new();

    public bool HasActiveAnimations => entries.Count > 0;

    public AnimationHandle Schedule<T>(UiObject target, UiProperty<T> property, Animation<T> animation)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(animation);

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i].Targets(target, property))
            {
                entries[i].Clear();
                entries.RemoveAt(i);
            }
        }

        AnimationEntry<T> entry = new(target, property, animation);
        entries.Add(entry);
        return new AnimationHandle(entry);
    }

    public AnimationTickResult Tick(TimeSpan elapsed)
    {
        int ticked = 0;
        int completed = 0;

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            IAnimationEntry entry = entries[i];
            if (entry.IsStopped)
            {
                entry.Clear();
                entries.RemoveAt(i);
                completed++;
                continue;
            }

            entry.Tick(elapsed);
            ticked++;

            if (entry.IsComplete)
            {
                entry.Clear();
                entries.RemoveAt(i);
                completed++;
            }
        }

        return new AnimationTickResult(ticked, completed, HasActiveAnimations);
    }

    internal interface IAnimationEntry
    {
        bool IsStopped { get; }

        bool IsComplete { get; }

        void Tick(TimeSpan elapsed);

        void Clear();

        void Stop();

        bool Targets(UiObject target, UiProperty property);
    }

    private sealed class AnimationEntry<T>(UiObject target, UiProperty<T> property, Animation<T> animation) : IAnimationEntry
    {
        public bool IsStopped { get; private set; }

        public bool IsComplete => animation.IsComplete;

        public void Tick(TimeSpan elapsed)
        {
            animation.Tick(elapsed);
            AnimatedValueSource.Apply(target, property, animation.CurrentValue);
        }

        public void Clear()
        {
            AnimatedValueSource.Clear(target, property);
        }

        public void Stop()
        {
            IsStopped = true;
        }

        public bool Targets(UiObject candidateTarget, UiProperty candidateProperty)
        {
            return ReferenceEquals(target, candidateTarget) && ReferenceEquals(property, candidateProperty);
        }
    }

    public sealed class AnimationHandle
    {
        private readonly IAnimationEntry entry;

        internal AnimationHandle(IAnimationEntry entry)
        {
            this.entry = entry;
        }

        public void Stop()
        {
            entry.Stop();
        }
    }
}

public readonly record struct AnimationTickResult(int Ticked, int Completed, bool HasPendingWork);
