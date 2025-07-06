using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LookAt : MonoBehaviour
{
    [field: SerializeField, Tooltip("If blank, uses Camera.main")]
    private Transform _target;


    private void Awake()
    {
        if (!_target)
        {
            _target = Camera.main.transform;
        }
    }
    
    private void Update()
    {
        if (_target)
        {
            transform.LookAt(_target);
        }
    }
}
