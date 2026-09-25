using System.Numerics;
using Cerneala.UI.Input;

namespace Cerneala.SceneVillage;

internal sealed class VillageInput
{
    private readonly HashSet<InputKey> held = [];

    internal bool Set(InputKey key, bool isDown)
    {
        if (key is not (InputKey.W or InputKey.A or InputKey.S or InputKey.D))
        {
            return false;
        }

        if (isDown)
        {
            held.Add(key);
        }
        else
        {
            held.Remove(key);
        }

        return true;
    }

    internal void Clear() => held.Clear();

    internal Vector2 Direction
    {
        get
        {
            float x = (held.Contains(InputKey.D) ? 1f : 0f) - (held.Contains(InputKey.A) ? 1f : 0f);
            float y = (held.Contains(InputKey.S) ? 1f : 0f) - (held.Contains(InputKey.W) ? 1f : 0f);
            var direction = new Vector2(x, y);
            return direction == Vector2.Zero ? direction : Vector2.Normalize(direction);
        }
    }
}
