using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField]
    private Slider _musicVolumeSlider;
    [SerializeField]
    private Slider _ambienceVolumeSlider;
    [SerializeField]
    private Slider _sfxVolumeSlider;
    [SerializeField]
    private Toggle _fullscreenToggle;


    private void OnEnable()
    {
        _musicVolumeSlider.SetValueWithoutNotify(MyExtensions.PerceptualDecibelsToVolume(AudioManager.Instance.GetMusicGroupGain()));
        _ambienceVolumeSlider.SetValueWithoutNotify(MyExtensions.PerceptualDecibelsToVolume(AudioManager.Instance.GetAmbienceGroupGain()));
        _sfxVolumeSlider.SetValueWithoutNotify(MyExtensions.PerceptualDecibelsToVolume(AudioManager.Instance.GetSFXGroupGain()));
        _fullscreenToggle.isOn = Screen.fullScreen;
    }

    private void Awake()
    {
        _musicVolumeSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        _ambienceVolumeSlider.onValueChanged.AddListener(OnAmbienceSliderChanged);
        _sfxVolumeSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        _fullscreenToggle.onValueChanged.AddListener(FullscreenToggled);
    }

    public void Button_Return()
    {
        PlayerPrefs.SetFloat("MusicVolume", _musicVolumeSlider.value);
        PlayerPrefs.SetFloat("AmbienceVolume", _ambienceVolumeSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", _sfxVolumeSlider.value);

        UIManager.Instance.ExitSettingsMenu();
    }

    private void FullscreenToggled(bool toggle)
    {
        Screen.fullScreen = toggle;
    }

    public void OnMusicSliderChanged(float sliderValue)
    {
        AudioManager.Instance.SetMusicGroupGain(MyExtensions.VolumeToPerceptualDecibels(sliderValue));
    }

    public void OnAmbienceSliderChanged(float sliderValue)
    {
        AudioManager.Instance.SetAmbienceGroupGain(MyExtensions.VolumeToPerceptualDecibels(sliderValue));
    }

    public void OnSFXSliderChanged(float sliderValue)
    {
        AudioManager.Instance.SetSFXGroupGain(MyExtensions.VolumeToPerceptualDecibels(sliderValue));
    }
}