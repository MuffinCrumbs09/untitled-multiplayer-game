using UnityEngine;

public class AttackStateBehaviour : StateMachineBehaviour
{
    // Called when the attack animation actually starts playing
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Tell the controller the animation has started
        animator.GetComponent<EnemyController>()?.OnAttackAnimationStarted();
    }

    // Called when the attack animation finishes and transitions out
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Tell the controller the animation is done
        animator.GetComponent<EnemyController>()?.OnAttackAnimationFinished();
    }
}