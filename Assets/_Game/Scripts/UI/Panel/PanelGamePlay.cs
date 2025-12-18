using UnityEngine;
using UnityEngine.UI;

public class PanelGamePlay : UICanvas
{
    [Header("UI")]
    [SerializeField] GameObject mergeButtonGO;   
    private float checkInterval = 0.15f;

    HeartChainManager _chain;
    float _nextCheckTime;
    bool _lastCanMerge;

    public Button boostBtn;
    public Text labelText;
    private string normalText = "Boost Update";
    private string maxText = "Boost Max LV";

    [Header("UI")]
    public Image iconImage;


    [Header("Visual when disabled")]
    public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    Image _btnImage;

    void Awake()
    {
        if (boostBtn == null) boostBtn = GetComponent<Button>();
        _btnImage = boostBtn.GetComponent<Image>();
        _chain = FindObjectOfType<HeartChainManager>();
    }
    
    void OnEnable()
    {
        Refresh();
        ForceRefreshMergeButton();
        RefreshState();
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

    
    public void OnClickUpgradeDrain()
    {
        var chain = FindObjectOfType<HeartChainManager>();
        if (chain == null || chain.GetLeader() == null) return;

        var energy = chain.GetLeader().GetComponent<HeartWithEnergy>();
        if (energy == null) return;

        bool success = energy.TryUpgradeDrain();
        if (!success)
        {
            SetMaxState();
            return;
        }

        RefreshState();
    }

    void RefreshState()
    {
        var chain = FindObjectOfType<HeartChainManager>();
        if (chain == null || chain.GetLeader() == null) return;

        var energy = chain.GetLeader().GetComponent<HeartWithEnergy>();
        if (energy == null) return;

        if (energy.IsDrainUpgradeMaxed())
            SetMaxState();
        else
            SetNormalState();
    }

    void SetNormalState()
    {
        if (labelText != null)
            labelText.text = normalText;

        if (boostBtn != null)
            boostBtn.interactable = true;
    }

    void SetMaxState()
    {
        if (labelText != null)
            labelText.text = maxText;

        if (boostBtn != null)
            boostBtn.interactable = false;

        if (_btnImage != null)
            _btnImage.color = disabledColor;
    }

    public void Refresh()
    {
        if (HeartManager.Instance == null)
        {
            
            return;
        }

        int level = HeartManager.Instance.GetAddableHeartLevel();

        int index = level - 1;
        if (index < 0 || index >= HeartManager.Instance.heartPrefabsByLevel.Count)
        {
            
            return; 
        }

        GameObject prefab = HeartManager.Instance.heartPrefabsByLevel[index];
        if (prefab == null)
        {
            
            return;
        }

        HeartStats stats = prefab.GetComponent<HeartStats>();
        if (stats == null || stats.icon == null)
        {
            
            return;
        }

        iconImage.sprite = stats.icon;
        iconImage.enabled = true;
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
    
    public void AddGateBTN()
    {
        GateManager.Instance?.SpawnGate();
    }
}
