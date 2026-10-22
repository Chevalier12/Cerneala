using Cerneala.UI.Core;
using Cerneala.UI.Motion.Core;

namespace Cerneala.UI.Motion.Properties;

public abstract class MotionPropertyBinding : IDisposable
{
    internal abstract MotionSystem Motion { get; }

    internal abstract bool IsActive { get; }

    // True while the binding holds a value written by a Motion @set, not an animation.
    internal virtual bool HoldsSetValue => false;

    public abstract UiObject Target { get; }

    public abstract UiProperty PropertyUntyped { get; }

    public abstract void Clear(MotionClearBehavior behavior = MotionClearBehavior.RestoreBase);

    public abstract void Dispose();
}
