using System;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private Button _lowQualityButton;
    [SerializeField]
    private Button _mediumQualityButton;
    [SerializeField]
    private Button _highQualityButton;
    [SerializeField]
    private Toggle _fullscreenToggle;

    private bool _wasFullscreen;


    private void OnEnable()
    {
        EventSystem.current.SetSelectedGameObject(_musicVolumeSlider.gameObject);

        _musicVolumeSlider.SetValueWithoutNotify(MyExtensions.PerceptualDecibelsToVolume(AudioManager.Instance.GetMusicGroupGain()));
        _ambienceVolumeSlider.SetValueWithoutNotify(
            MyExtensions.PerceptualDecibelsToVolume(AudioManager.Instance.GetAmbienceGroupGain()));
        _sfxVolumeSlider.SetValueWithoutNotify(MyExtensions.PerceptualDecibelsToVolume(AudioManager.Instance.GetSFXGroupGain()));
        _fullscreenToggle.isOn = Screen.fullScreen;
    }

    private void Awake()
    {
        _musicVolumeSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        _ambienceVolumeSlider.onValueChanged.AddListener(OnAmbienceSliderChanged);
        _sfxVolumeSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        _fullscreenToggle.onValueChanged.AddListener(FullscreenToggled);

        _wasFullscreen = Screen.fullScreen;
    }

    private void Update()
    {
        if (_wasFullscreen && !Screen.fullScreen)
        {
            _wasFullscreen = false;
            PlayerPrefs.SetInt("Fullscreen", 0); // save fullscreen off
        }
        else if (!_wasFullscreen && Screen.fullScreen)
        {
            _wasFullscreen = true;
            PlayerPrefs.SetInt("Fullscreen", 1); // save fullscreen on
        }
    }

    public void Button_Return()
    {
        UIManager.Instance.ExitSettingsMenu(true);
    }

    public void Button_SetHighQuality()
    {
        SettingsManager.SetQualitySetting(ITCQualitySetting.High);
    }

    public void Button_SetMediumQuality()
    {
        SettingsManager.SetQualitySetting(ITCQualitySetting.Medium);
    }

    public void Button_SetLowQuality()
    {
        SettingsManager.SetQualitySetting(ITCQualitySetting.Low);
    }

    public void OnExitSettings()
    {
        SettingsManager.SaveAudioPreferences(_musicVolumeSlider.value, _ambienceVolumeSlider.value, _sfxVolumeSlider.value);
        gameObject.SetActive(false);
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