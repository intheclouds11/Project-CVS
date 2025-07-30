using System;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;


    private void Awake()
    {
        Instance = this;
        LoadGraphicsPreferences();
    }

    private void Start()
    {
        LoadAudioPreferences();
    }

    private static void LoadGraphicsPreferences()
    {
        if (PlayerPrefs.HasKey("Fullscreen"))
            Screen.fullScreen = PlayerPrefs.GetInt("Fullscreen") == 1;

        if (PlayerPrefs.HasKey("QualitySetting"))
            QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("QualitySetting"));
    }

    private static void LoadAudioPreferences()
    {
        if (PlayerPrefs.HasKey("MusicVolume"))
            AudioManager.Instance.SetMusicGroupGain(MyExtensions.VolumeToPerceptualDecibels(PlayerPrefs.GetFloat("MusicVolume")));
        if (PlayerPrefs.HasKey("AmbienceVolume"))
            AudioManager.Instance.SetAmbienceGroupGain(
                MyExtensions.VolumeToPerceptualDecibels(PlayerPrefs.GetFloat("AmbienceVolume")));
        if (PlayerPrefs.HasKey("SFXVolume"))
            AudioManager.Instance.SetSFXGroupGain(MyExtensions.VolumeToPerceptualDecibels(PlayerPrefs.GetFloat("SFXVolume")));
    }

    public static void SetQualitySetting(ITCQualitySetting qualitySetting)
    {
        var targetQuality = (int) qualitySetting;
        if (QualitySettings.GetQualityLevel() == targetQuality) return;
        QualitySettings.SetQualityLevel(targetQuality);
        PlayerPrefs.SetInt("QualitySetting", targetQuality);
    }

    public static void SaveAudioPreferences(float musicVol, float ambienceVol, float sfxVol)
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVol);
        PlayerPrefs.SetFloat("AmbienceVolume", ambienceVol);
        PlayerPrefs.SetFloat("SFXVolume", sfxVol);
    }
}