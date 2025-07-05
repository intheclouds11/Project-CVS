using UnityEngine;

public class FirstBossEncounter : MonoBehaviour
{
    [SerializeField]
    private AudioClip _bossMusic;

    public void EnteredBossZone()
    {
        AudioManager.Instance.MusicAudioSource.clip = _bossMusic;
    }
}
