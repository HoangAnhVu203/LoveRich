using System.Collections;
using UnityEngine;

public class Gate : MonoBehaviour
{
    public int totalMoney;

    HeartChainManager chain;

    void OnTriggerEnter(Collider other)
    {
        var stats = other.GetComponent<HeartStats>();
        if (stats == null) return;

        totalMoney += stats.moneyValue;

        if (stats.gateHitVFX != null)
        {
            Instantiate(stats.gateHitVFX, other.transform.position, stats.gateHitVFX.transform.rotation);
        }

        if (chain != null && other.transform == chain.GetLeader())
        {
            var rider = FindObjectOfType<RiderAnimator>();
            if (rider != null) rider.PlayGateHit();
        }
    }
}
