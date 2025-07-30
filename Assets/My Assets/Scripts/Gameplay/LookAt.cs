using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class LookAt : MonoBehaviour
{
    [field: SerializeField, Tooltip("If blank, uses Camera.main")]
    protected Transform _target;
    public bool FlipDirection;

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
            var dir = FlipDirection ? -_target.forward : _target.forward;
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(LookAt))]
public class LookAtEditor : Editor
{
    private SerializedProperty _targetProp;
    private SerializedProperty _flipProp;
    
    
    private void OnEnable()
    {
        _targetProp = serializedObject.FindProperty("_target");
        _flipProp = serializedObject.FindProperty("FlipDirection");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        if (!_targetProp.objectReferenceValue)
        {
            EditorGUILayout.HelpBox("No Target. Will use main camera transform", MessageType.Info);
        }

        EditorGUILayout.PropertyField(_targetProp);
        EditorGUILayout.PropertyField(_flipProp);
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
