using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadManager : Singleton<RoadManager>
{
    [Header("Road prefabs (mỗi prefab có SplinePath)")]
    public List<GameObject> roadPrefabs = new();
    public Transform roadRoot;

    [Header("Refs")]
    public HeartChainManager chain;

    int _index = 0;
    readonly List<GameObject> _instances = new();

    void Awake()
    {
        if (chain == null) chain = FindObjectOfType<HeartChainManager>();
        if (roadRoot == null) roadRoot = transform;
    }

    void Start()
    {
        for (int i = 0; i < roadPrefabs.Count; i++)
        {
            var go = Instantiate(roadPrefabs[i], roadRoot);
            go.name = roadPrefabs[i].name + "_Instance";
            go.SetActive(false);
            _instances.Add(go);
        }

        StartCoroutine(SwitchRoadCR(0));
    }

    public void NextRoad()
    {
        if (_instances.Count == 0) return;
        _index = (_index + 1) % _instances.Count;
        StartCoroutine(SwitchRoadCR(_index));
    }

    IEnumerator SwitchRoadCR(int idx)
    {
        // chỉ bật 1 road
        for (int i = 0; i < _instances.Count; i++)
            _instances[i].SetActive(i == idx);

        // đợi 1 frame để road mới OnEnable/Awake ổn định
        yield return null;

        var spline = _instances[idx].GetComponentInChildren<SplinePath>(true);
        if (spline == null)
        {
            Debug.LogError("[RoadManager] Road thiếu SplinePath");
            yield break;
        }

        spline.Rebuild();

        if (chain != null)
            chain.SetSplinePathKeepState(spline);

        Debug.Log($"[RoadManager] Switched to road {idx}: {_instances[idx].name}");
    }
}
