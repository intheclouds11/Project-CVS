using System;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerAnimator : MonoBehaviour
{
    public Animator Animator { get; private set; }


    private void Awake()
    {
        Animator = GetComponent<Animator>();
    }

    public void SetSpeed(float speed) => Animator.SetFloat("Speed", speed);
    public void SetIsGrounded(bool grounded) => Animator.SetBool("IsGrounded", grounded);
    public void SetReadyAttackTrigger() => Animator.SetTrigger("Charge");
    public void SetAttackTrigger() => Animator.SetTrigger("Attack");
    public void SetDiedTrigger() => Animator.SetTrigger("Died");
    public void SetIsDashing(bool dashing) => Animator.SetBool("IsDashing", dashing);

    public void ThrowSawBlade()
    {
        GameManager.Instance.Player1.PlayerAttack.ThrowSawBlade();
    }
}