using UnityEngine;

public class Gate : MonoBehaviour
{
    [SerializeField] HeartChainManager chain;
    [SerializeField] RiderAnimator rider;

    void Awake()
    {
        if (chain == null) chain = FindObjectOfType<HeartChainManager>();
        if (rider == null) rider = FindObjectOfType<RiderAnimator>();
    }

    void OnTriggerEnter(Collider other)
    {
        var stats = other.GetComponent<HeartStats>();
        if (stats == null) return;

        PlayerMoney.Instance?.AddMoney(stats.moneyValue);

        if (stats.gateHitVFX != null)
        {
            Instantiate(
                stats.gateHitVFX,
                other.transform.position,
                stats.gateHitVFX.transform.rotation
            );
        }

        
        if (chain != null && other.transform == chain.GetLeader())
        {
            rider?.PlayGateHit(); // trong đây đã spawn VFX nhân vật
        }
    }
}
