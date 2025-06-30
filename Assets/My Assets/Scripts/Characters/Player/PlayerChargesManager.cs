using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[Serializable]
public class PlayerCharge
{
    internal GameObject ChargeGameObject;
    [SerializeField]
    internal float RechargeTimer;
}

public class PlayerChargesManager : MonoBehaviour
{
    [SerializeField]
    private bool _enemyHitRecharges = true;
    [SerializeField]
    private int _maxCharges = 1;
    [SerializeField]
    private float _rechargeTime = 2f;
    [SerializeField]
    private AudioClip _rechargeSFX;
    [SerializeField]
    private List<GameObject> _chargeGameObjects;
    [SerializeField]
    private List<PlayerCharge> _charges = new();

    [ShowNonSerializedField]
    private int _remainingCharges;


    private void Awake()
    {
        // Setup Charges
        for (int i = 0; i < _chargeGameObjects.Count; i++)
        {
            var chargeGO = _chargeGameObjects[i];
            chargeGO.SetActive(_maxCharges > i);
            _charges[i].ChargeGameObject = chargeGO;
        }

        ForceFullRecharge();
    }

    private void OnEnable()
    {
        SawBlade.EnemyHit += SawBladeOnEnemyHit;
    }

    private void OnDisable()
    {
        SawBlade.EnemyHit -= SawBladeOnEnemyHit;
    }

    private void SawBladeOnEnemyHit()
    {
        if (_remainingCharges < _maxCharges)
        {
            for (int i = _charges.Count - 1; i >= 0; i--)
            {
                var chargeToRestore = _charges[i];
                if (!chargeToRestore.ChargeGameObject.activeSelf)
                {
                    RestoreCharge(chargeToRestore);
                    return;
                }
            }
        }
    }

    private void Update()
    {
        if (_remainingCharges < _maxCharges)
        {
            for (int i = _charges.Count - 1; i >= 0; i--)
            {
                var currentCharge = _charges[i];
                if (currentCharge.ChargeGameObject.activeSelf) continue; // Only increment recharge timer for first used charge
                if (currentCharge.RechargeTimer >= _rechargeTime)
                {
                    RestoreCharge(currentCharge);
                }
                else
                {
                    currentCharge.RechargeTimer += Time.deltaTime;
                }

                return;
            }
        }
    }

    public bool IsChargeAvailable()
    {
        return _remainingCharges > 0;
    }

    public void UseCharge()
    {
        _remainingCharges--;

        for (int i = _charges.Count - 1; i >= 0; i--)
        {
            var playerCharge = _charges[i];
            var chargeGO = playerCharge.ChargeGameObject;
            if (chargeGO.activeSelf)
            {
                playerCharge.RechargeTimer = 0f;
                chargeGO.SetActive(false);
                return;
            }
        }
    }

    public void ForceFullRecharge()
    {
        _remainingCharges = _maxCharges;
        foreach (var playerCharge in _charges)
        {
            playerCharge.ChargeGameObject.SetActive(true);
            playerCharge.RechargeTimer = 0f;
        }
    }

    private void RestoreCharge(PlayerCharge playerCharge)
    {
        _remainingCharges++;
        playerCharge.ChargeGameObject.SetActive(true);
        AudioManager.Instance.PlaySound(transform, _rechargeSFX, true, false, 0.5f, 1.2f);
    }
}