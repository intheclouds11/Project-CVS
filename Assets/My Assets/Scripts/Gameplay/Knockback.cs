using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
public class Knockback
{
    [SerializeField]
    public bool ApplyKnockback = true;
    [SerializeField]
    public float KnockbackAmount = 1f;
    [SerializeField]
    public float KnockbackDuration = 0.25f;
    [SerializeField]
    public float StunDuration = 0.25f;
    [SerializeField]
    public Ease KnockbackEasing = Ease.InExpo;
    [HideInInspector]
    public Vector3 Direction;
}