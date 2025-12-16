using UnityEngine;

public class PanelGamePlay : UICanvas
{
    [Header("UI")]
    [SerializeField] GameObject mergeButtonGO;   
    private float checkInterval = 0.15f;

    HeartChainManager _chain;
    float _nextCheckTime;
    bool _lastCanMerge;

    void Awake()
    {
        _chain = FindObjectOfType<HeartChainManager>();
    }

    void OnEnable()
    {
        ForceRefreshMergeButton();
    }

    void Update()
    {
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + checkInterval;

        RefreshMergeButtonIfNeeded();
    }

    void ForceRefreshMergeButton()
    {
        _lastCanMerge = CanMergeTriple();
        if (mergeButtonGO != null) mergeButtonGO.SetActive(_lastCanMerge);
    }

    void RefreshMergeButtonIfNeeded()
    {
        bool canMerge = CanMergeTriple();
        if (canMerge == _lastCanMerge) return;

        _lastCanMerge = canMerge;
        if (mergeButtonGO != null) mergeButtonGO.SetActive(canMerge);
    }

    bool CanMergeTriple()
    {
        if (_chain == null || _chain.hearts == null) return false;

        var list = _chain.hearts;
        int n = list.Count;
        if (n < 3) return false;

        for (int i = 0; i <= n - 3; i++)
        {
            var a = list[i] ? list[i].GetComponent<HeartStats>() : null;
            var b = list[i + 1] ? list[i + 1].GetComponent<HeartStats>() : null;
            var c = list[i + 2] ? list[i + 2].GetComponent<HeartStats>() : null;

            if (a == null || b == null || c == null) continue;

            if (a.type == b.type && b.type == c.type)
                return true;
        }
        return false;
    }

    public void AddHeartBTN()
    {
        HeartManager.Instance.AddHeart();
        ForceRefreshMergeButton();
    }

    public void MergeHeartBTN()
    {
        HeartManager.Instance.MergeAnyTriple();
        ForceRefreshMergeButton();
    }

    public void NextRoadBTN()
    {
        RoadManager.Instance?.NextRoad();
    }
}
