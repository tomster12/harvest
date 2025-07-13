using UnityEngine;

public static class Easing
{
    public static float EaseInSine(float t) => 1 - Mathf.Cos(t * Mathf.PI / 2);

    public static float EaseOutSine(float t) => Mathf.Sin(t * Mathf.PI / 2);

    public static float EaseOutQuad(float t) => 1 - (1 - t) * (1 - t);

    public static float EaseInQuad(float t) => t * t;

    public static float EaseOutExponential(float t)
    {
        if (t == 1) return 1;
        return 1 - Mathf.Pow(2, -10 * t);
    }

    public static float EaseInExponential(float t)
    {
        if (t == 0) return 0;
        return Mathf.Pow(2, 10 * (t - 1));
    }
}
