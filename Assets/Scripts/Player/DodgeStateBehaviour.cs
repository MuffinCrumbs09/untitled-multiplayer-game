using UnityEngine;

/// <summary>
/// A StateMachineBehaviour that notifies the PlayerMovement component when the
/// dodge animation state is entered and exited.
/// </summary>
public class DodgeStateBehaviour : StateMachineBehaviour
{
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponent<PlayerMovement>()?.BeginDodge();
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponent<PlayerMovement>()?.EndDodge();
    }
}