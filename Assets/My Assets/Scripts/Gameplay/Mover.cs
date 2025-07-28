using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Mover : MonoBehaviour
{
    [SerializeField]
    private Vector3 _moveToPositionLocal;
    [SerializeField]
    private float _moveDuration = 1f;
    [SerializeField]
    private AudioClip _moveSFX;
    [SerializeField]
    private float _moveSFXVolume = 0.7f;

    private bool _startMoving;
    private AudioSource _moveAudio;
    private float _distToMove;


    private void Update()
    {
        if (!_startMoving) return;
        if (transform.localPosition == _moveToPositionLocal)
        {
            _moveAudio.Stop();
            _moveAudio = null;
            enabled = false;
        }


        transform.localPosition =
            Vector3.MoveTowards(transform.localPosition, _moveToPositionLocal, _distToMove / _moveDuration * Time.deltaTime);
    }

    public void StartMoving()
    {
        _startMoving = true;
        _moveAudio = AudioManager.Instance.PlaySound(transform, _moveSFX, true, false, _moveSFXVolume);
        _distToMove = Vector3.Distance(transform.localPosition, _moveToPositionLocal);
    }
}