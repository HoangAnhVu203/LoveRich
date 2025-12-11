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
    public GameObject heartPinkPrefab;       
    public GameObject heartLightBluePrefab; 
    public int needCountToMerge = 3;       

    [Tooltip("Tên Layer dùng cho HeartPink (để nhận diện)")]
    public string pinkLayerName = "HeartPink"; 

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

        // XÓA 3 heart cuối (dùng count ban đầu)
        for (int i = count - 1; i >= count - 3; i--)
        {
            Transform h = list[i];
            list.RemoveAt(i);
            if (h != null)
                Destroy(h.gameObject);
        }

        if (heartLightBluePrefab == null)
        {
            return;
        }

        GameObject newHeart = Instantiate(
            heartLightBluePrefab,
            spawnPos,
            spawnRot,
            spawnParent
        );

        newHeart.transform.localScale = h1.localScale;

        // Xử lý HeartWithEnergy cho heart mới
        var energy = newHeart.GetComponent<HeartWithEnergy>();

        // Nếu sau khi xoá 3 con, list đang RỖNG → heart mới là LEADER
        if (list.Count == 0)
        {
            if (energy == null)
            {
                energy = newHeart.AddComponent<HeartWithEnergy>();
            }

            // Leader phải được phép boost
            energy.enabled = true;

            // Gán center nếu cần
            if (energy.center == null && center != null)
                energy.center = center;
        }
        else
        {
            // Vẫn còn leader cũ ở list[0] -> heart mới chỉ là follower
            if (energy != null)
                energy.enabled = false;
        }

        // Gắn vào chuỗi (last)
        list.Add(newHeart.transform);

        Debug.Log($"[Merge] Merge OK: trước {count} → sau {list.Count}");
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
