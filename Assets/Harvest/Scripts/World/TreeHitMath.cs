using UnityEngine;

public static class TreeHitMath
{
    public static float SwingQuality(float accuracy)
    {
        return Mathf.Lerp(MIN_SWING_QUALITY, 1f, Mathf.Clamp01(accuracy));
    }

    public static float Hardness(float baseHardness, float baseRadius, float radius, float minRadius)
    {
        float span = Mathf.Max(0.0001f, baseRadius - minRadius);
        float depthFraction = Mathf.Clamp01((baseRadius - radius) / span);
        float sizeFactor = Mathf.Sqrt(Mathf.Max(0.0001f, baseRadius) / REFERENCE_RADIUS);
        float coreFactor = 1f + (CORE_HARDNESS_MULTIPLIER - 1f) * depthFraction * depthFraction;
        return baseHardness * sizeFactor * coreFactor;
    }

    public static float LandChance(float effectiveSharpness, float hardness)
    {
        float capability = Mathf.Max(0.0001f, effectiveSharpness * SHARPNESS_CAPABILITY_SCALE);
        float raw = 1f / (1f + Mathf.Pow(hardness / capability, LAND_CURVE_STEEPNESS));

        // Fade the low tail to exactly zero so unchoppable wood is truly unchoppable
        float tail = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(LAND_TAIL_MIN, LAND_TAIL_MAX, raw));
        return raw * tail;
    }

    public static float DentDepth(float effectiveStrength, float hardness)
    {
        float ratio = Mathf.Clamp(effectiveStrength / Mathf.Max(0.0001f, hardness), MIN_DENT_RATIO, MAX_DENT_RATIO);
        return DEPTH_SCALE * ratio;
    }

    public static float WoodDamage(float effectiveStrength, float landChance)
    {
        float marginalMultiplier = Mathf.Clamp(1f / Mathf.Max(0.0001f, landChance), 1f, MAX_MARGINAL_DAMAGE_MULTIPLIER);
        return DAMAGE_BASE * (effectiveStrength / REFERENCE_STRENGTH) * marginalMultiplier;
    }

    public static float HalfHeightFromBitWidth(float bitWidth) => bitWidth * BIT_HEIGHT_RATIO;

    private static readonly float MIN_SWING_QUALITY = 0.25f;
    private static readonly float REFERENCE_RADIUS = 0.35f;
    private static readonly float CORE_HARDNESS_MULTIPLIER = 5f;
    private static readonly float SHARPNESS_CAPABILITY_SCALE = 5f;
    private static readonly float LAND_CURVE_STEEPNESS = 1.4f;
    private static readonly float LAND_TAIL_MIN = 0.03f;
    private static readonly float LAND_TAIL_MAX = 0.08f;
    private static readonly float DEPTH_SCALE = 0.0235f;
    private static readonly float MIN_DENT_RATIO = 0.25f;
    private static readonly float MAX_DENT_RATIO = 3f;
    private static readonly float DAMAGE_BASE = 1f;
    private static readonly float REFERENCE_STRENGTH = 5f;
    private static readonly float MAX_MARGINAL_DAMAGE_MULTIPLIER = 4f;
    private static readonly float BIT_HEIGHT_RATIO = 0.125f;
}
