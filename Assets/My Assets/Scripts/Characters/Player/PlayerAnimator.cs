using System;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;

    void Awake() => animator = GetComponent<Animator>();

    public void SetSpeed(float speed) => animator.SetFloat("Speed", speed);
    public void SetIsGrounded(bool grounded) => animator.SetBool("IsGrounded", grounded);
    public void SetReadyAttackTrigger() => animator.SetTrigger("ReadyAttack");
    public void SetAttackTrigger() => animator.SetTrigger("Attack");
    public void SetIsDashing(bool dashing) => animator.SetBool("IsDashing", dashing);
}