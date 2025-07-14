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
}
