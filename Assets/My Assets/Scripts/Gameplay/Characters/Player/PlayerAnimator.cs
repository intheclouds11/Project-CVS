using System;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator _animator;

    
    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void SetSpeed(float speed) => _animator.SetFloat("Speed", speed);
    public void SetIsGrounded(bool grounded) => _animator.SetBool("IsGrounded", grounded);
    public void SetReadyAttackTrigger() => _animator.SetTrigger("ReadyAttack");
    public void SetAttackTrigger() => _animator.SetTrigger("Attack");
    public void SetDiedTrigger() => _animator.SetTrigger("Died");
    public void SetIsDashing(bool dashing) => _animator.SetBool("IsDashing", dashing);
}