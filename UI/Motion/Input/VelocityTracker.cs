namespace Cerneala.UI.Motion.Input;

public sealed class VelocityTracker
{
    private float lastX;
    private float lastY;
    private TimeSpan lastTime;
    private bool hasSample;

    public float VelocityX { get; private set; }

    public float VelocityY { get; private set; }

    public void Reset(float x, float y, TimeSpan time)
    {
        lastX = x;
        lastY = y;
        lastTime = time;
        VelocityX = 0;
        VelocityY = 0;
        hasSample = true;
    }

    public void Add(float x, float y, TimeSpan time)
    {
        if (!hasSample)
        {
            Reset(x, y, time);
            return;
        }

        double seconds = (time - lastTime).TotalSeconds;
        if (seconds > 0)
        {
            VelocityX = (float)((x - lastX) / seconds);
            VelocityY = (float)((y - lastY) / seconds);
        }

        lastX = x;
        lastY = y;
        lastTime = time;
    }
}
