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

    private void Update()
    {
        if (InputManager.Instance.ActivateExpressionUpWasPressed)
        {
            SetFemalePoseTrigger();
        }
        else if (InputManager.Instance.ActivateExpressionDownWasPressed)
        {
            SetHiphopTrigger();
            // Animator.applyRootMotion = true;
        }
        else if (InputManager.Instance.ActivateExpressionLeftWasPressed)
        {
            SetHurricaneTrigger();
            // Animator.applyRootMotion = true;
        }
        else if (InputManager.Instance.ActivateExpressionRightWasPressed)
        {
            SetSillyTrigger();
            // Animator.applyRootMotion = true;
        }

        // if (Animator.GetFloat("Speed") > 0.1f)
        // {
        //     Animator.applyRootMotion = false;
        // }
    }

    public void SetSpeed(float speed) => Animator.SetFloat("Speed", speed);
    public void SetIsGrounded(bool grounded) => Animator.SetBool("IsGrounded", grounded);
    // public void SetSpawning(bool spawning, bool silly) => Animator.SetBool(silly ? "SillySpawning" : "Spawning", spawning);
    public void SetReadyAttackTrigger() => Animator.SetTrigger("Charge");
    public void SetFemalePoseTrigger() => Animator.SetTrigger("FemalePose");
    public void SetHiphopTrigger() => Animator.SetTrigger("Hiphop");
    public void SetHurricaneTrigger() => Animator.SetTrigger("Hurricane");
    public void SetSillyTrigger() => Animator.SetTrigger("Silly");
    public void SetAttackTrigger() => Animator.SetTrigger("Attack");
    public void SetDeathTrigger() => Animator.SetTrigger("Death");
    public void SetHitReactTrigger() => Animator.SetTrigger("HitReact");
    public void SetIsDashing(bool dashing) => Animator.SetBool("IsDashing", dashing);

    public void ThrowSawBlade()
    {
        GameManager.Instance.Player1.PlayerAttack.ThrowSawBlade();
    }
}