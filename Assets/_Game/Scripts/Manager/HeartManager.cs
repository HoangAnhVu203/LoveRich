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

    [Header("Heart Prefabs By Level")]
    public List<GameObject> heartPrefabsByLevel;

    [Tooltip("Tên Layer dùng cho HeartPink ")]
    public string pinkLayerName = "HeartPink";

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
    }

    void Start()
    {
        if (HeartUnlocks.Instance != null)
        {
            var chain = FindObjectOfType<HeartChainManager>();
            if (chain != null && chain.hearts != null)
            {
                foreach (var t in chain.hearts)
                {
                    if (t == null) continue;
                    var s = t.GetComponent<HeartStats>();
                    if (s != null) HeartUnlocks.Instance.MarkUnlocked(s.type);
                }
            }
        }
    }

    public void AddHeart()
    {
        var manager = HeartChainManagerInstance;
        if (manager == null || manager.hearts.Count == 0) return;

        Transform last = manager.hearts[manager.hearts.Count - 1];
        if (last == null) return;

        int spawnLevel = GetAddableHeartLevel();

        int index = spawnLevel - 1;
        if (index < 0 || index >= heartPrefabsByLevel.Count)
        {
            Debug.LogError($"[AddHeart] Không có prefab cho level {spawnLevel}");
            return;
        }

        GameObject prefab = heartPrefabsByLevel[index];

        GameObject newHeart = Instantiate(
            prefab,
            last.position,
            last.rotation,
            spawnParent
        );

        newHeart.transform.localScale = last.localScale;

        var energy = newHeart.GetComponent<HeartWithEnergy>();
        if (energy != null) energy.enabled = false;

        manager.RegisterHeart(newHeart.transform);
        manager.RecalculateLeaderByWeight();
        manager.EnsureEnergyOnLeaderOnly();

        Debug.Log($"[AddHeart] Spawn heart level {spawnLevel}");
    }


    int FindFirstMergeTripleIndex(out HeartType foundType)
    {
        foundType = default;
        var chain = HeartChainManagerInstance;
        if (chain == null || chain.hearts == null) return -1;

        var list = chain.hearts;
        int n = list.Count;
        if (n < 3) return -1;

        for (int i = 0; i <= n - 3; i++)
        {
            var s0 = list[i].GetComponent<HeartStats>();
            var s1 = list[i + 1].GetComponent<HeartStats>();
            var s2 = list[i + 2].GetComponent<HeartStats>();

            if (s0 == null || s1 == null || s2 == null) continue;

            if (s0.type == s1.type && s1.type == s2.type)
            {
                foundType = s0.type;
                return i;
            }
        }

        return -1;
    }

    HeartChainManager HeartChainManagerInstance
    {
        get { return FindObjectOfType<HeartChainManager>(); }
    }

    // ======== MERGE ANY TRIPLE ========

    public void MergeAnyTriple()
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

        // 1. Tìm cụm 3 liên tiếp cùng loại
        HeartType tripleType;
        int startIndex = FindFirstMergeTripleIndex(out tripleType);
        if (startIndex < 0)
        {
            Debug.Log("[Merge] Không có cụm 3 heart liên tiếp cùng loại.");
            return;
        }

        Transform h0 = list[startIndex];
        Transform h1 = list[startIndex + 1];
        Transform h2 = list[startIndex + 2];

        if (h0 == null || h1 == null || h2 == null) return;

        HeartStats stats = h0.GetComponent<HeartStats>();
        if (stats == null)
        {
            Debug.LogWarning("[Merge] Thiếu HeartStats trên heart.");
            return;
        }

        if (stats.mergeResultPrefab == null)
        {
            Debug.LogWarning("[Merge] mergeResultPrefab chưa được gán cho loại " + stats.type);
            return;
        }

        long oldMoney = stats.moneyValue;


        // 2. Vị trí spawn = trung bình 3 tim
        Vector3 spawnPos = (h0.position + h1.position + h2.position) / 3f;
        Quaternion spawnRot = h1.rotation;

        // 3. Xoá 3 tim khỏi list & scene (từ index lớn về nhỏ)
        // NOTE: remove khỏi list trước để tránh logic khác đọc nhầm
        for (int i = startIndex + 2; i >= startIndex; i--)
        {
            Transform h = list[i];
            list.RemoveAt(i);
            if (h != null)
                Destroy(h.gameObject);
        }

        // 4. Tạo tim mới
        GameObject newHeart = Instantiate(
            stats.mergeResultPrefab,
            spawnPos,
            spawnRot,
            spawnParent
        );

        newHeart.transform.localScale = h1.localScale;

        var newStats = newHeart.GetComponent<HeartStats>();

        HeartUnlocks.Instance.TryUpdateMaxLevel(newStats.level);

        var panel = FindObjectOfType<PanelGamePlay>(true);
        if (panel != null)
            panel.Refresh();

        // 5. KHÔNG tự quyết Energy ở đây (để chain quyết sau khi recalc)
        var energy = newHeart.GetComponent<HeartWithEnergy>();
        if (energy != null) energy.enabled = false;

        // 6. Thêm tim mới vào chuỗi tại vị trí startIndex
        list.Insert(startIndex, newHeart.transform);

        Debug.Log($"[Merge] Merge 3 {tripleType} tại index {startIndex} → {stats.mergeResultPrefab.name}");

        // 7. Recalc leader + đảm bảo energy đúng
        chain.RecalculateLeaderByWeight();
        chain.EnsureEnergyOnLeaderOnly();

        // 8. Reset history + snap để node nối ngay, không bị đứng/khựng
        //chain.RebuildHistoryByChainSegments();
        //chain.SnapAllHeartsToHistory();
        chain.SnapChainImmediate();

        if (newStats != null && HeartUnlocks.Instance != null)
        {
            if (!HeartUnlocks.Instance.IsUnlocked(newStats.type))
            {
                HeartUnlocks.Instance.MarkUnlocked(newStats.type);

                var panels = UIManager.Instance.OpenUI<PanelNewHeart>();

                Sprite icon = newStats.icon;
                panels.Show(icon, newStats.level, oldMoney, newStats.moneyValue);
            }
        }

    }

    // ======== Helper lấy leader / last từ ChainManager ========

    public Transform GetLastHeart()
    {
        var chain = HeartChainManagerInstance;
        if (chain == null || chain.hearts.Count == 0)
            return null;

        return chain.hearts[chain.hearts.Count - 1];
    }

    public Transform GetLeader()
    {
        var chain = HeartChainManagerInstance;
        if (chain == null || chain.hearts.Count == 0)
            return null;

        return chain.hearts[0];
    }

    public int GetAddableHeartLevel()
    {
        if (HeartUnlocks.Instance == null)
            return 1;

        int maxUnlocked = HeartUnlocks.Instance.GetMaxUnlockedLevel();
        int maxAddable = maxUnlocked - 3;

        if (maxAddable < 1)
            maxAddable = 1;

        var chain = HeartChainManagerInstance;
        if (chain == null || chain.hearts == null || chain.hearts.Count == 0)
            return maxAddable;

        int lowestLevelInChain = int.MaxValue;

        foreach (var t in chain.hearts)
        {
            if (t == null) continue;

            var stats = t.GetComponent<HeartStats>();
            if (stats == null) continue;

            if (stats.level < lowestLevelInChain)
                lowestLevelInChain = stats.level;
        }

        // Nếu còn heart thấp hơn level addable max → add heart đó
        if (lowestLevelInChain < maxAddable)
            return lowestLevelInChain;

        // Nếu tất cả >= maxAddable → add maxAddable
        return maxAddable;
    }


}
