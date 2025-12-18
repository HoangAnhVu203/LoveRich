// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;

// public class GameManager : MonoBehaviour
// {
//     public static GameManager Instance;

//     const string SAVE_KEY = "GAME_SAVE_DATA";

//     [Header("Runtime Data")]
//     [SerializeField] long money;
//     public long Money
//     {
//         get => money;
//         set
//         {
//             if (money == value) return;
//             money = value;
//             MarkDirty();
//         }
//     }

//     [Header("Auto Save")]
//     [SerializeField] float autoSaveInterval = 10f;

//     bool _dirty;
//     float _dirtyTimer;

//     // ================= UNITY =================

//     void Awake()
//     {
//         if (Instance != null && Instance != this)
//         {
//             Destroy(gameObject);
//             return;
//         }

//         Instance = this;
//         DontDestroyOnLoad(gameObject);
//     }

//     void Start()
//     {
//         StartCoroutine(LoadGameCR());
//     }

//     void Update()
//     {
//         if (!_dirty) return;

//         _dirtyTimer += Time.deltaTime;
//         if (_dirtyTimer >= autoSaveInterval)
//         {
//             _dirtyTimer = 0f;
//             _dirty = false;
//             SaveGame();
//         }
//     }

//     void OnApplicationPause(bool pause)
//     {
//         if (pause)
//             SaveGame();
//     }

//     void OnApplicationQuit()
//     {
//         SaveGame();
//     }

//     // ================= DIRTY =================

//     public void MarkDirty()
//     {
//         _dirty = true;
//     }

//     // ================= SAVE =================

//     public void SaveGame()
//     {
//         GameSaveData data = new GameSaveData
//         {
//             hearts = new List<HeartSaveData>(),
//             gates  = new List<GateSaveData>()
//         };

//         data.money = money;

//         SaveRoad(data);
//         SaveHearts(data);
//         SaveGates(data);

//         string json = JsonUtility.ToJson(data);
//         PlayerPrefs.SetString(SAVE_KEY, json);
//         PlayerPrefs.Save();

//         Debug.Log("[AUTO SAVE] " + json);
//     }

//     // ================= LOAD =================

//     IEnumerator LoadGameCR()
//     {
//         yield return null;

//         if (!PlayerPrefs.HasKey(SAVE_KEY))
//         {
//             Debug.Log("[LOAD] No save found");
//             yield break;
//         }

//         string json = PlayerPrefs.GetString(SAVE_KEY);
//         GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

//         money = data.money;

//         LoadRoad(data);
//         LoadHearts(data);
//         LoadGates(data);

//         Debug.Log("[LOAD OK]");
//     }

//     // ================= HEART =================

//     void SaveHearts(GameSaveData data)
//     {
//         var chain = FindObjectOfType<HeartChainManager>();
//         if (chain == null) return;

//         foreach (var t in chain.hearts)
//         {
//             if (t == null) continue;

//             var stats = t.GetComponent<HeartStats>();
//             if (stats == null) continue;

//             data.hearts.Add(new HeartSaveData
//             {
//                 level = stats.level,
//                 type  = stats.type
//             });
//         }
//     }

//     void LoadHearts(GameSaveData data)
//     {
//         var chain = FindObjectOfType<HeartChainManager>();
//         if (chain == null) return;

//         chain.ClearAllHearts();

//         foreach (var h in data.hearts)
//         {
//             GameObject prefab =
//                 HeartManager.Instance.heartPrefabsByLevel[h.level - 1];

//             var heart = Instantiate(prefab);
//             chain.RegisterHeart(heart.transform);
//         }

//         chain.RecalculateLeaderByWeight();
//         chain.EnsureEnergyOnLeaderOnly();
//         chain.SnapChainImmediate();
//     }

//     // ================= ROAD =================

//     void SaveRoad(GameSaveData data)
//     {
//         if (RoadManager.Instance != null)
//             data.currentRoadIndex = RoadManager.Instance.CurrentRoadIndex;
//     }

//     void LoadRoad(GameSaveData data)
//     {
//         RoadManager.Instance?.LoadRoad(data.currentRoadIndex);
//     }

//     // ================= GATE =================

//     void SaveGates(GameSaveData data)
//     {
//         if (GateManager.Instance == null) return;

//         foreach (var e in GateManager.Instance.GetAllGateEntries())
//         {
//             data.gates.Add(new GateSaveData
//             {
//                 roadIndex = e.roadIndex,
//                 ratio     = e.ratio
//             });
//         }
//     }

//     void LoadGates(GameSaveData data)
//     {
//         if (GateManager.Instance == null) return;

//         GateManager.Instance.ClearAllGates();

//         foreach (var g in data.gates)
//         {
//             GateManager.Instance.SpawnGateAt(
//                 g.roadIndex,
//                 g.ratio
//             );
//         }
//     }
// }
