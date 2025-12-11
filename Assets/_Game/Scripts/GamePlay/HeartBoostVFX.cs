using UnityEngine;

[DisallowMultipleComponent]
public class HeartBoostVFX : MonoBehaviour
{
    public GameObject boostVFX;

    HeartChainManager _chain;

    void Awake()
    {
        _chain = FindObjectOfType<HeartChainManager>();
        
        if (boostVFX != null)
            boostVFX.SetActive(false); 
    }

    void Update()
    {
        if (boostVFX == null || _chain == null) return;
        if (_chain.hearts == null || _chain.hearts.Count == 0) return;

        bool isLastHeart = (_chain.hearts[_chain.hearts.Count - 1] == transform);

        bool isBoosting = HeartWithEnergy.IsBoostingGlobal;

        bool show = isBoosting && isLastHeart;

        if (boostVFX.activeSelf != show)
        {
            boostVFX.SetActive(show);
        }
    }

    void OnDisable()
    {
        if (boostVFX != null)
            boostVFX.SetActive(false);
    }
}
