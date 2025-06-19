using UnityEngine;

public class PermanentObjectInitialization : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializePermanentObjects()
    {
        Instantiate(Resources.Load<GameObject>("StartupObjects"));
    }
}
