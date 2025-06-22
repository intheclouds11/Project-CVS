using System;
using System.Collections;
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

    private CanvasGroup _canvasGroup;
    private bool _startedRespawn;
    private float _lastTimeShown;


    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        _lastTimeShown = Time.time;
        _canvasGroup.alpha = 0f;
        _blackImage.color = new Color(_blackImage.color.r, _blackImage.color.g, _blackImage.color.b, 0f);
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

        GameManager.Instance.OnReturnToMainMenu();
        GameManager.Instance.GameStart();

        gameObject.SetActive(false);
        _canvasGroup.alpha = 0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}