using System.Collections.Generic;
using UnityEngine;

public class HeartManager : MonoBehaviour
{
    public static HeartManager Instance;

    public GameObject heartPrefab;
    public Transform spawnParent;
    public Transform center;        
    public float followSmooth = 8f;

    [Header("Danh sách Hearts")]
    public List<Transform> hearts = new List<Transform>();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        UpdateHeartFollow();
    }

    void UpdateHeartFollow()
    {
        for (int i = 1; i < hearts.Count; i++)
        {
            Transform prev = hearts[i - 1];
            Transform cur = hearts[i];

            cur.position = Vector3.Lerp(
                cur.position,
                prev.position,
                followSmooth * Time.deltaTime
            );
        }
    }

    // Gọi khi bấm nút
    public void AddHeart()
    {
        if (HeartChainManagerInstance == null) return;

        var manager = HeartChainManagerInstance;

        // Lấy vị trí của heart cuối cùng
        Transform last = manager.hearts[manager.hearts.Count - 1];

        GameObject newHeart = Instantiate(
            heartPrefab,
            last.position,
            last.rotation,
            spawnParent
        );
        var energy = newHeart.GetComponent<HeartWithEnergy>();
        if (energy != null) Destroy(energy);

        manager.RegisterHeart(newHeart.transform);
    }

    HeartChainManager HeartChainManagerInstance
    {
        get { return FindObjectOfType<HeartChainManager>(); }
    }

    // Lấy heart cuối để bật VFX
    public Transform GetLastHeart()
    {
        if (hearts.Count == 0) return null;
        return hearts[hearts.Count - 1];
    }

    public Transform GetLeader()
    {
        if (hearts.Count == 0) return null;
        return hearts[0];
    }
}
