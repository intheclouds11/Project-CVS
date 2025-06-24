using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RespawnScreen : MonoBehaviour
{
    [SerializeField]
    private float _delayRespawnInputTime = 1f;
    [SerializeField]
    private float _fadeInTime = 3f;
    [SerializeField]
    private float _fadeOutTime = 2f;
    [SerializeField]
    private Image _blackImage;
    [SerializeField]
    private TextMeshProUGUI _respawnText;

    private CanvasGroup _canvasGroup;
    private bool _startedRespawn;
    private float _lastTimeShown;


    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        StartCoroutine(OnSceneLoadedCoroutine());
    }

    private void OnEnable()
    {
        _lastTimeShown = Time.time;
        _canvasGroup.alpha = 0f;
        _blackImage.color = new Color(_blackImage.color.r, _blackImage.color.g, _blackImage.color.b, 0f);
        _respawnText.enabled = true;
    }

    private void Update()
    {
        if (_lastTimeShown + _delayRespawnInputTime < +Time.time && InputManager.Instance.RespawnWasPressed)
        {
            StartCoroutine(RespawnCoroutine());
        }

        if (_canvasGroup.alpha < 1)
        {
            _canvasGroup.alpha += Time.deltaTime / _fadeInTime;
        }
    }

    private IEnumerator RespawnCoroutine()
    {
        var startAlpha = _blackImage.color.a;

        while (_blackImage.color.a < 1)
        {
            startAlpha += Time.deltaTime / _fadeOutTime;
            _blackImage.color = new Color(_blackImage.color.r, _blackImage.color.g, _blackImage.color.b, startAlpha);
            yield return null;
        }

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        GameManager.Instance.OnReturnToMainMenu();
        GameManager.Instance.GameStart();
    }

    private IEnumerator OnSceneLoadedCoroutine()
    {
        GameManager.Instance.Player1.ResetCamera();
        
        yield return new WaitForSeconds(0.1f);
        
        // _respawnText.enabled = false;
        // while (_canvasGroup.alpha > 0f)
        // {
        //     _canvasGroup.alpha -= Time.deltaTime / _fadeOutTime;
        //     yield return null;
        // }
        
        _canvasGroup.alpha = 0f;
        yield return null;
        gameObject.SetActive(false);
    }
}