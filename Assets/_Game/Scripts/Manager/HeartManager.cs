using System.Collections.Generic;
using UnityEngine;

public class HeartManager : MonoBehaviour
{
    public static HeartManager Instance;

    [Header("Prefab mặc định khi Add")]
    public GameObject heartPrefab;          
    public Transform spawnParent;
    public Transform center;        
    public float followSmooth = 8f;

    [Header("Merge Settings")]
    public GameObject heartPinkPrefab;        // prefab HeartPink
    public GameObject heartLightBluePrefab;   // prefab HeartLightBlue
    public int needCountToMerge = 3;          // cần mấy heart để merge (3)

    [Tooltip("Tên Layer dùng cho HeartPink (để nhận diện)")]
    public string pinkLayerName = "HeartPink"; // đặt đúng layer của HeartPink


    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        
    }

    [System.Obsolete]
    public void AddHeart()
    {
        if (HeartChainManagerInstance == null) return;

        var manager = HeartChainManagerInstance;

        if (manager.hearts.Count == 0)
        {
            Debug.LogWarning("[HeartManager] ChainManager chưa có leader.");
            return;
        }

        Transform last = manager.hearts[manager.hearts.Count - 1];

        // Spawn vào ROOT
        GameObject newHeart = Instantiate(
            heartPrefab,
            last.position,
            last.rotation,
            spawnParent 
        );

        // COPY SCALE CHUẨN TỪ HEART CŨ
        newHeart.transform.localScale = last.localScale;

        // follower không cần Energy
        var energy = newHeart.GetComponent<HeartWithEnergy>();
        if (energy != null)
            energy.enabled = false;


        manager.RegisterHeart(newHeart.transform);
    }




    [System.Obsolete]
    HeartChainManager HeartChainManagerInstance
    {
        get { return FindObjectOfType<HeartChainManager>(); }
    }

    // ======== MERGE PINK → LIGHT BLUE ========

    // Gắn hàm này vào Button "Merge"
    [System.Obsolete]
    public void MergeLast3Pink()
    {
        var chain = HeartChainManagerInstance;
        if (chain == null || chain.hearts == null)
            return;

        List<Transform> list = chain.hearts;
        int count = list.Count;

        if (count < 3)
        {
            Debug.Log("[Merge] Chưa đủ 3 heart.");
            return;
        }

        int pinkLayer = LayerMask.NameToLayer(pinkLayerName);
        if (pinkLayer < 0)
        {
            Debug.LogWarning("[Merge] Layer HeartPink không tồn tại.");
            return;
        }

        // Lấy 3 heart cuối theo count ban đầu
        int i0 = count - 3;
        int i1 = count - 2;
        int i2 = count - 1;

        Transform h0 = list[i0];
        Transform h1 = list[i1];
        Transform h2 = list[i2];

        // Nếu không phải cả 3 cùng Pink → không merge
        if (h0.gameObject.layer != pinkLayer ||
            h1.gameObject.layer != pinkLayer ||
            h2.gameObject.layer != pinkLayer)
        {
            Debug.Log("[Merge] 3 heart cuối không cùng layer Pink → không merge.");
            return;
        }

        // Lưu vị trí spawn = heart ở giữa
        Vector3 spawnPos = h1.position;
        Quaternion spawnRot = h1.rotation;

        // XÓA 3 heart cuối
        // Lưu ý: dùng count ban đầu, không dùng list.Count trong điều kiện
        for (int i = count - 1; i >= count - 3; i--)
        {
            Transform h = list[i];
            list.RemoveAt(i);
            if (h != null)
                Destroy(h.gameObject);
        }

        // Tạo heart mới
        if (heartLightBluePrefab == null)
        {
            Debug.LogWarning("[Merge] Chưa gán prefab heartLightBluePrefab.");
            return;
        }

        GameObject newHeart = Instantiate(
        heartLightBluePrefab,
        spawnPos,
        spawnRot,
        spawnParent 
        );

        // Copy scale chuẩn
        newHeart.transform.localScale = h1.localScale;

        // follower không cần energy
        var energy = newHeart.GetComponent<HeartWithEnergy>();
        if (energy != null)
            Destroy(energy);

        // Gắn vào chuỗi
        list.Add(newHeart.transform);

    }

    // ======== Helper lấy leader / last từ ChainManager ========

    [System.Obsolete]
    public Transform GetLastHeart()
    {
        var chain = HeartChainManagerInstance;
        if (chain == null || chain.hearts.Count == 0)
            return null;

        return chain.hearts[chain.hearts.Count - 1];
    }

    [System.Obsolete]
    public Transform GetLeader()
    {
        var chain = HeartChainManagerInstance;
        if (chain == null || chain.hearts.Count == 0)
            return null;

        return chain.hearts[0];
    }
}
