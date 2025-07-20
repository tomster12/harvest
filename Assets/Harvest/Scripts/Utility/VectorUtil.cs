using UnityEngine;

public static class VectorUtil
{
    public static bool IsAngleBetween(float theta, float a1, float a2)
    {
        if (a1 <= a2) return theta >= a1 && theta < a2;
        else return theta >= a1 || theta < a2;
    }

    public static float GetAngleDifference(float a1, float a2) => Mathf.Min((a1 - a2 + Mathf.PI * 2f) % (Mathf.PI * 2f), (a2 - a1 + Mathf.PI * 2f) % (Mathf.PI * 2f));

    public static float GetPosAngle(Vector3 p) => (Mathf.Atan2(p.z, p.x) + (Mathf.PI * 2)) % (Mathf.PI * 2);

    public static bool RaySegmentIntersection(Vector2 a, Vector2 b, Vector2 c, float angle, out Vector2 intersection)
    {
        intersection = default;

        Vector2 segDir = b - a;
        Vector2 rayDir = new((float)Mathf.Cos(angle), (float)Mathf.Sin(angle));
        Vector2 ac = c - a;

        // Ensure segment and ray aren't parallel
        float det = segDir.y * rayDir.x - segDir.x * rayDir.y;
        if (Mathf.Abs(det) < 1e-6f) return false;

        // Calculate the intersection point between segment and ray
        float t = (ac.y * rayDir.x - ac.x * rayDir.y) / det;
        float s = (ac.y * segDir.x - ac.x * segDir.y) / det;

        if (t >= 0f && t <= 1f && s >= 0f)
        {
            intersection = a + t * segDir;
            return true;
        }

        return false;
    }
}
