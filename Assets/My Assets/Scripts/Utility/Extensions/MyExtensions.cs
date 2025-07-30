using UnityEngine;

public static class MyExtensions
{
    public static void GetCylinderPoints(this Transform t, Vector3 center, float height, float radius, out Vector3 p1, out Vector3 p2)
    {
        Vector3 centerWorldPos = t.TransformPoint(center);
        float cylinderLength = Mathf.Max(0, height * 0.5f - radius);
        p1 = centerWorldPos + Vector3.up * cylinderLength;
        p2 = centerWorldPos - Vector3.up * cylinderLength;
    }

    public static float PerceptualDecibelsToVolume(float dbValue, float minDb = -80f)
    {
        dbValue = Mathf.Clamp(dbValue, minDb, 0f);

        // Convert dB to linear gain
        float gain = Mathf.Pow(10f, dbValue / 20f);

        // Calculate min gain
        float minGain = Mathf.Pow(10f, minDb / 20f);

        // Inverse lerp on the gain scale
        float volume = Mathf.InverseLerp(minGain, 1f, gain);
        return volume;
    }

    public static float VolumeToPerceptualDecibels(float volume, float minDb = -80f)
    {
        volume = Mathf.Clamp01(volume);

        // 1. Define the minimum gain corresponding to your min dB
        float minGain = Mathf.Pow(10f, minDb / 20f); // e.g., -80 dB → ~0.0001 gain

        // 2. Interpolate on gain scale (not dB)
        float gain = Mathf.Lerp(minGain, 1f, volume);

        // 3. Convert back to decibels
        float db = 20f * Mathf.Log10(gain);
        return db;
    }

    public static Vector3 RemovePitch(this Vector3 direction)
    {
        return new Vector3(direction.x, 0f, direction.z).normalized;
    }

    public static Vector3 GetVector(this ITCAxis axis)
    {
        if (axis == ITCAxis.X)
        {
            return Vector3.right;
        }

        if (axis == ITCAxis.Y)
        {
            return Vector3.up;
        }

        if (axis == ITCAxis.Z)
        {
            return Vector3.forward;
        }

        if (axis == ITCAxis.NegX)
        {
            return -Vector3.right;
        }

        if (axis == ITCAxis.NegY)
        {
            return -Vector3.up;
        }

        return -Vector3.forward;
    }
}