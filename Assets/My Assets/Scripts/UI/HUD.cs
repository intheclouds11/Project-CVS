using System;
using UnityEngine;

public class HUD : MonoBehaviour
{
    public static HUD Instance;
    
    [SerializeField]
    private GameObject _winUI;
    [SerializeField]
    private GameObject _waveCompleteUI;
    [SerializeField]
    private GameObject _loseUI;


    private void Awake()
    {
        Instance = this;
    }

    public GameObject GetWinUI => _winUI;
    public GameObject GetLoseUI => _loseUI;
    public GameObject GetWaveCompleteUI => _waveCompleteUI;
}
