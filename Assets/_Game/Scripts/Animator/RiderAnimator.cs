using UnityEngine;

public class RiderAnimator : MonoBehaviour
{
    public Animator animator;

    [Header("Money / Action VFX")]
    public GameObject actionVFX;          
    public Transform vfxFollowPoint;      
    public float vfxLifeTime = 2f;

    static readonly int GateHit = Animator.StringToHash("GateHit");

    public void PlayGateHit()
    {
        animator?.SetTrigger(GateHit);
        PlayActionVFX();
    }

    void PlayActionVFX()
    {
        if (actionVFX == null || vfxFollowPoint == null) return;

        var vfx = Instantiate(
            actionVFX,
            vfxFollowPoint.position,
            vfxFollowPoint.rotation
        );

        // cho VFX đi theo nhân vật
        vfx.transform.SetParent(vfxFollowPoint);

        Destroy(vfx, vfxLifeTime);
    }
}
