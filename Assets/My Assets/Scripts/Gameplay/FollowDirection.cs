using System;
using UnityEngine;
using UnityEngine.ProBuilder;

public enum VectorAxis
{
    x,
    y,
    z
}

public class FollowDirection : MonoBehaviour
{
    public VectorAxis vectorAxis;
    public Transform target;
    public float smoothing;

    private float _offset;

    private void Awake()
    {
        if (vectorAxis == VectorAxis.x)
            _offset = transform.position.x - target.position.x;
        else if (vectorAxis == VectorAxis.y)
            _offset = transform.position.y - target.position.y;
        else if (vectorAxis == VectorAxis.z)
            _offset = transform.position.z - target.position.z;
    }

    private void Update()
    {
        var pos = transform.position;
        if (vectorAxis == VectorAxis.x)
            pos.x = target.position.x + _offset;
        else if (vectorAxis == VectorAxis.y)
            pos.y = target.position.y + _offset;
        else if (vectorAxis == VectorAxis.z)
            pos.z = target.position.z + _offset;

        transform.position = Vector3.MoveTowards(transform.position, pos, smoothing * Time.deltaTime);
    }
}