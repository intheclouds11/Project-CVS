using UnityEngine;

public class Follower : MonoBehaviour
{
    private Transform _target;
    
    public void SetTarget(Transform target)
    {
        _target = target;
    }
    
    private void Update()
    {
        if (_target)
        {
            transform.position = _target.position;
        }
    }
}
