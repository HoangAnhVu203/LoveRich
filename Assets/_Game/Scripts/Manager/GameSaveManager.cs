using System.Collections;
using System.Linq;
using UnityEngine;

public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance { get; private set; }
    public event System.Action OnGameLoaded;

    const string SAVE_KEY = "LOVELOOP_SAVE_V1";

    [Header("Autosave")]
    [SerializeField] bool autosaveOnPause = true;
    [SerializeField] bool autosaveOnQuit = true;

    [Header("Load Timing")]
    [SerializeField] bool loadOnStart = true;

    bool _loadedOnce;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (loadOnStart)
            StartCoroutine(LoadGameCR());
    }

    void OnApplicationPause(bool pause)
    {
        if (autosaveOnPause && pause) SaveGame();
    }

    void OnApplicationQuit()
    {
        if (autosaveOnQuit) SaveGame();
    }

    IEnumerator LoadGameCR()
    {
        if (_loadedOnce) yield break;
        _loadedOnce = true;

        // Đợi 1-2 frame để RoadManager/GateManager/HeartManager kịp Awake/Start
        yield return null;
        yield return null;

        // Đợi RoadManager đã build road instances
        float timeout = Time.realtimeSinceStartup + 3f;
        while (RoadManager.Instance == null || !RoadManager.Instance.IsReady)
        {
            if (Time.realtimeSinceStartup > timeout) break;
            yield return null;
        }

        LoadGame();
    }

    public void SaveGame()
    {
        var data = BuildSaveData();
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
        Debug.Log("[Save] OK");
    }

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
        {
            Debug.Log("[Load] No save found");
            return;
        }

        string json = PlayerPrefs.GetString(SAVE_KEY, "");
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("[Load] Save empty");
            return;
        }

        var data = JsonUtility.FromJson<SaveData>(json);
        if (data == null)
        {
            Debug.LogWarning("[Load] Save parse failed");
            return;
        }

        Debug.Log($"[Load] gates count = {(data.gates != null ? data.gates.Count : -1)}");

        ApplySaveData(data);
        Debug.Log("[Load] OK");
    }

    SaveData BuildSaveData()
    {
        var data = new SaveData();

        // Money / Rose
        if (PlayerMoney.Instance != null) data.money = PlayerMoney.Instance.currentMoney;
        if (RoseWallet.Instance != null) data.rose = RoseWallet.Instance.CurrentRose;

        // Road upgrade store
        data.unlockedRoadCount = RoadUpgradeStore.GetUnlockedRoadCount();
        data.roadUpgradeCount = RoadUpgradeStore.GetUpgradeCount();

        // Current road + per-road gate counts
        if (RoadManager.Instance != null)
        {
            data.currentRoadIndex = RoadManager.Instance.CurrentRoadIndex;

            var map = RoadManager.Instance.ExportRoadGateCounts();
            data.roadGateCounts = map
                .Select(kv => new RoadGateCountSave { roadIndex = kv.Key, count = kv.Value })
                .ToList();
        }

        // Chain + Hearts (levels)
        var chain = FindObjectOfType<HeartChainManager>();
        if (chain != null)
        {
            data.leaderDistance = chain.leaderDistance;
            data.reverseDirection = chain.reverseDirection;

            data.heartLevels.Clear();
            if (chain.hearts != null)
            {
                foreach (var t in chain.hearts)
                {
                    if (!t) continue;
                    var stats = t.GetComponent<HeartStats>();
                    data.heartLevels.Add(stats != null ? stats.level : 1);
                }
            }
        }

        // Gates snapshot
        if (GateManager.Instance != null && RoadManager.Instance != null)
        {
            data.gates = GateManager.Instance.ExportAllGates();
        }

        Debug.Log($"[Save] gates count = {(data.gates != null ? data.gates.Count : -1)}");

        // GateCostStore
        data.gatePurchasedCount = GateCostStore.GetPurchasedGateCount();
        data.gateLastCost = GateCostStore.GetLastCost();

        // ActionCostStore (lưu giá hiện tại)
        data.addCost = ActionCostStore.GetAddCost();
        data.mergeCost = ActionCostStore.GetMergeCost();

        return data;
    }

    void ApplySaveData(SaveData data)
    {
        // 1) restore stores trước
        GateCostStore.SetPurchasedGateCount(data.gatePurchasedCount);
        GateCostStore.SetLastCost(data.gateLastCost);

        RoadUpgradeStore.SetUnlockedRoadCount(data.unlockedRoadCount);
        RoadUpgradeStore.SetUpgradeCount(data.roadUpgradeCount);

        ActionCostStore.SetAddCost(data.addCost);
        ActionCostStore.SetMergeCost(data.mergeCost);

        // 2) money/rose
        if (PlayerMoney.Instance != null)
            PlayerMoney.Instance.SetMoney(data.money);

        if (RoseWallet.Instance != null)
            RoseWallet.Instance.SetRose(data.rose);

        // 3) Road + per-road gate counts
        if (RoadManager.Instance != null)
        {
            RoadManager.Instance.ImportRoadGateCounts(data.roadGateCounts);
            RoadManager.Instance.SwitchToRoadImmediate(data.currentRoadIndex);
        }

        // 4) Rebuild hearts
        var chain = FindObjectOfType<HeartChainManager>();
        if (chain != null)
        {
            chain.reverseDirection = data.reverseDirection;

            // rebuild chain objects from levels
            if (HeartManager.Instance != null)
                HeartManager.Instance.RebuildChainFromLevels(data.heartLevels);

            // restore distance + snap
            chain.leaderDistance = data.leaderDistance;
            chain.SnapChainImmediate();
        }

        // 5) Rebuild gates (KHÔNG trừ rose/cost)
        if (GateManager.Instance != null)
        {
            GateManager.Instance.LoadGatesFromSave(data.gates);
        }

        // 6) refresh
        GameManager.Instance?.RefreshLapPreview();

        // refresh panel nếu đang mở
        var panel = FindObjectOfType<PanelGamePlay>(true);
        if (panel != null) panel.Refresh();

        OnGameLoaded?.Invoke();

        StartCoroutine(RebindCameraAfterLoadCR());

        Debug.Log($"[Load] apply GateCostStore purchasedCount={data.gatePurchasedCount} lastCost={data.gateLastCost}");

    }

    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        Debug.Log("[Save] Cleared");
    }


    [Header("Debounced Save")]
    [SerializeField] float debounceDelay = 0.15f; 

    Coroutine _saveCR;
    bool _pending;

    public void RequestSave()
    {
        _pending = true;

        if (_saveCR == null)
            _saveCR = StartCoroutine(SaveDebounceCR());
    }

    IEnumerator SaveDebounceCR()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, debounceDelay));

        if (_pending)
        {
            _pending = false;
            SaveGame();
        }

        _saveCR = null;
    }

    public void SaveNow()
    {
        _pending = false;
        if (_saveCR != null)
        {
            StopCoroutine(_saveCR);
            _saveCR = null;
        }
        SaveGame();
    }

    IEnumerator RebindCameraAfterLoadCR()
    {
        yield return null;
        yield return null; 
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.RebindToLeaderSnap();
    }

}
