using UnityEngine;

[DisallowMultipleComponent]
public class HeartBoostVFX : MonoBehaviour
{
    public GameObject boostVFX;

    [System.Obsolete]
    void Update()
    {
        if (boostVFX == null) return;
        var chain = FindObjectOfType<HeartChainManager>();
        if (chain == null || chain.hearts.Count == 0) return;

        bool isHolding = Input.GetMouseButton(0) || Input.touchCount > 0;
        bool isLastHeart = (chain.hearts[chain.hearts.Count - 1] == transform);
        bool show = isHolding && isLastHeart;

        if (boostVFX.activeSelf != show)
        {
            boostVFX.SetActive(show);
        }
    }
}
