using UnityEngine;

public class RiderFollowLeader : MonoBehaviour
{
    [Header("Refs")]
    public HeartChainManager chain;
    public Vector3 localOffset = new Vector3(0f, 0.25f, 0f);
    public Vector3 localEulerOffset = Vector3.zero;

    [Header("Smooth")]
    public float posLerpNormal = 20f;
    public float rotLerpNormal = 20f;
    public float posLerpBoost  = 60f;
    public float rotLerpBoost  = 60f;

    public float blendSpeed = 10f; // 8–15

    float _pLerp, _rLerp;


    Transform _leader;

    void Awake()
    {
        _pLerp = posLerpNormal;
        _rLerp = rotLerpNormal;
        if (chain == null) chain = FindObjectOfType<HeartChainManager>();
    }

    void LateUpdate()
    {
        var leader = chain.GetLeader();
        if (leader == null) return;

        Vector3 targetPos = leader.TransformPoint(localOffset);
        Quaternion targetRot = leader.rotation * Quaternion.Euler(localEulerOffset);

        bool boosting = HeartWithEnergy.IsBoostingGlobal;
        float pTarget = boosting ? posLerpBoost : posLerpNormal;
        float rTarget = boosting ? rotLerpBoost : rotLerpNormal;

        _pLerp = Mathf.Lerp(_pLerp, pTarget, blendSpeed * Time.deltaTime);
        _rLerp = Mathf.Lerp(_rLerp, rTarget, blendSpeed * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, targetPos, _pLerp * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _rLerp * Time.deltaTime);

    }
}
