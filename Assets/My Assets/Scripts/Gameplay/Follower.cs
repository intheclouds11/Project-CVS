using UnityEngine;

public class Follower : MonoBehaviour
{
    [SerializeField]
    private Transform _target;
    [SerializeField]
    private Vector3 _offset;
    [SerializeField]
    private bool _lookTowards;
    
    
    public void SetTarget(Transform target)
    {
        _target = target;
    }
    
    private void Update()
    {
        if (_target)
        {
            transform.position = _target.position + _offset;
            if (_lookTowards) transform.LookAt(_target);
        }
        else
        {
            _target = GameManager.Instance?.Player1?.transform;
        }
    }
}
