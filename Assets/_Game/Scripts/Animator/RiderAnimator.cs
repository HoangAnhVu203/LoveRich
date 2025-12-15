using UnityEngine;

public class RiderAnimator : MonoBehaviour
{
    public Animator animator;
    static readonly int GateHit = Animator.StringToHash("GateHit");

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    public void PlayGateHit()
    {

        Debug.Log("Hit gate");
        if (animator == null) return;
        animator.ResetTrigger(GateHit);
        animator.SetTrigger(GateHit);
    }
}
