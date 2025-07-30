using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BaseEnemy), true)]
public class BaseEnemyEditor : Editor
{
    private BaseEnemy _baseEnemy;
    private SerializedProperty _wanderProp;
    private SerializedProperty _sleepBubbleProp;
    
    
    private void OnEnable()
    {
        _baseEnemy = target as BaseEnemy;
        _wanderProp = serializedObject.FindProperty("_wander");
        _sleepBubbleProp = serializedObject.FindProperty("_sleepBubble");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (Application.isPlaying) return;
        
        var sleepBubble = _sleepBubbleProp.objectReferenceValue as GameObject;
        if (_wanderProp.boolValue && sleepBubble is {activeSelf: true})
        {
            sleepBubble.SetActive(false);
        }
        else if (!_wanderProp.boolValue && sleepBubble is {activeSelf: false})
        {
            sleepBubble.SetActive(true);
        }
    }
}