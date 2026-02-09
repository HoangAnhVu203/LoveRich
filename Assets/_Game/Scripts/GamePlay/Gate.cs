using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Gate : MonoBehaviour
{
    [SerializeField] HeartChainManager chain;
    [SerializeField] RiderAnimator rider;

    [Header("Reward")]
    [SerializeField] string characterId;
    [SerializeField] int defaultLv = 1;

    [SerializeField] float cooldownPerHeart = 0.2f;

    [Header("Gate Light VFX")]
    [SerializeField] ParticleSystem lightVFX;
    [SerializeField] float lightDuration = 0.25f;

    Coroutine _lightCR;

    readonly Dictionary<int, float> _lastHitTime = new();

    public void SetCharacter(string id, int defaultLevel = 1)
    {
        characterId = id;
        defaultLv = Mathf.Max(1, defaultLevel);
    }

    void Awake()
    {
        if (chain == null) chain = FindObjectOfType<HeartChainManager>();
        if (rider == null) rider = FindObjectOfType<RiderAnimator>();
    }

    void OnTriggerEnter(Collider other)
    {
        var stats = other.GetComponentInParent<HeartStats>();
        if (stats == null) return;

        if (!other.CompareTag("Heart")) return;

        int key = other.GetInstanceID();
        float now = Time.time;
        if (_lastHitTime.TryGetValue(key, out float t) && now - t < cooldownPerHeart)
            return;
        _lastHitTime[key] = now;
        
        // ===== Gate Light VFX =====
        PlayGateLightVFX();


        // VFX
        if (stats.gateHitVFX != null)
            Instantiate(stats.gateHitVFX, other.transform.position, stats.gateHitVFX.transform.rotation);

        // Rider anim when leader hits
        if (chain != null && chain.GetLeader() != null && other.transform == chain.GetLeader())
            rider?.PlayGateHit();

        // ===== Reward =====
        long baseReward = stats.moneyValue;

        int lv = CharacterProgressStore.GetLevel(characterId, defaultLv);
        float mul = CharacterRevenueBonus.GetMultiplierByLevel(lv);

        long finalReward = Mathf.RoundToInt(baseReward * mul);
        PlayerMoney.Instance?.AddMoney(finalReward);

        Debug.Log($"[GateReward] charId='{characterId}' lv={lv} base={baseReward} mul={mul} final={finalReward}");

        GameManager.Instance?.RefreshLapPreview();
    }

    void PlayGateLightVFX()
    {
        if (!lightVFX) return;

        if (_lightCR != null)
            StopCoroutine(_lightCR);

        _lightCR = StartCoroutine(GateLightCR());
    }

    IEnumerator GateLightCR()
    {
        lightVFX.Clear(true);
        lightVFX.Play(true);

        yield return new WaitForSeconds(lightDuration);

        lightVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        _lightCR = null;
    }

}
