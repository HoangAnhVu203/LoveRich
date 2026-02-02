using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelGamePlay : UICanvas
{
    [Header("UI")]
    [SerializeField] GameObject mergeButtonGO;
    [SerializeField] Text moneyText;

    [Header("Cost UI")]
    [SerializeField] TMP_Text addCostText;
    [SerializeField] TMP_Text mergeCostText;
    string costPrefix = "$ ";

    [Header("Heart Cap UI")]
    [SerializeField] TMP_Text heartCapText;
    private string capFormat = "{0}/{1}";
    private float checkInterval = 0.15f;

    [Header("Lap Money UI")]
    [SerializeField] Text lapMoneyText;

    HeartChainManager _chain;
    float _nextCheckTime;
    bool _lastCanMerge;

    [Header("Boost")]
    public Button boostBtn;
    public Text labelText;
    private string normalText = "Boost Update";
    private string maxText = "Boost Max LV";

    [Header("Add Heart Icon")]
    public Image iconImage;

    [Header("Visual when disabled")]
    private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Merge Preview UI")]
    [SerializeField] Image mergeFromIcon;
    [SerializeField] Image mergeToIcon;

    [Header("Rose UI")]
    [SerializeField] TMP_Text roseText;

    [Header("Gate UI")]
    [SerializeField] TMP_Text gateCostText;
    [SerializeField] Button addGateBtn;

    [Header("Upgrade Road UI")]
    [SerializeField] TMP_Text upgradeRoadCostText;
    [SerializeField] Button upgradeRoadBtn;
    [SerializeField] TMP_Text gateProgressText;   
    string gateProgressFormat = "{0}/{1}";

    [Header("Flirt Book UI")]
    [SerializeField] GameObject flirtBookButtonGO;

    [Header("Boost Button")]
    [SerializeField] Button boostAdsBtn;
    [SerializeField] Text boostTxt; 
    [SerializeField] Image boostFillImage;
    [SerializeField] Image offerIconImage;   
    [SerializeField] Sprite boostSprite;
    [SerializeField] Sprite roseSprite;
    [SerializeField] Sprite heartSprite;

    [Header("Boost Offer Icons (separate)")]
    [SerializeField] private Image roseIconImage;
    [SerializeField] private Image heartIconImage;



    [Header("Building Claim Item")]
    [SerializeField] GameObject collectButtonGO;   
    [SerializeField] Button collectButton;  
    [SerializeField] Text upgradeText;


    [Header("Building Book UI")]
    [SerializeField] GameObject buildingBookButtonGO;

    [Header("Rose Fly FX")]
    [SerializeField] UIFlyIconFX roseFlyFX;
    [SerializeField] RectTransform addBtnRT;
    [SerializeField] RectTransform mergeBtnRT;
    [SerializeField] RectTransform roseTargetRT; 

    [Header("Upgrade Drain FX")]
    [SerializeField] RectTransform upgradeTextRT;
    [SerializeField] CanvasGroup upgradeTextCG;
    private float upgradeTextMoveY = 200f;
    private float upgradeTextDuration = 2.0f;

    Vector2 _upgradeTextStartPos;
    Coroutine _upgradeTextCR;

    [Header("Claim FX")]
    [SerializeField] RectTransform claimTextRT;
    [SerializeField] CanvasGroup claimTextCG;
    [SerializeField] Text claimText;                 
    private float claimTextMoveY = 200f;
     private float claimTextDuration = 2.0f;

    Vector2 _claimTextStartPos;
    Coroutine _claimTextCR;

    [Header("Boost Offer FX (separate)")]
    [SerializeField] RectTransform boostTextRT;
    [SerializeField] CanvasGroup boostTextCG;
    [SerializeField] Text boostText;              
    private float boostTextMoveY = 200f;
    private float boostTextDuration = 2.0f;

    Vector2 _boostTextStartPos;
    Coroutine _boostTextCR;



    // ================= ROTATING BOOST OFFER =================
    private enum BoostOffer
    {
        Boost60s,
        Rose20,
        Heart10
    }

    [SerializeField] private float offerSwitchInterval = 60f;
    [SerializeField] private BoostOffer[] offerCycle = new BoostOffer[]
    {
        BoostOffer.Boost60s,
        BoostOffer.Rose20,
        BoostOffer.Heart10
    };

    private int _offerIndex = 0;
    private float _nextOfferSwitchTime = 0f;

  
    private float pulseScale = 1.15f;
    private float pulseSpeed = 6f;
    Coroutine _pulseCR;   

    float _nextCheck;

    Image _btnImage;
    private float _offerStartTime;

    void Awake()
    {
        _chain = FindObjectOfType<HeartChainManager>();
        if (boostBtn != null) _btnImage = boostBtn.GetComponent<Image>();

        if (collectButtonGO == null) collectButtonGO = gameObject;
        if (collectButton == null) collectButton = collectButtonGO.GetComponent<Button>();

        if (collectButton != null)
        {
            collectButton.onClick.RemoveAllListeners();
            collectButton.onClick.AddListener(OnCollectClick);
        }

        if (upgradeTextRT != null)
        _upgradeTextStartPos = upgradeTextRT.anchoredPosition;

        if (upgradeTextCG != null)
            upgradeTextCG.alpha = 1f;

        if (upgradeTextRT != null)
            upgradeTextRT.gameObject.SetActive(false);

        if (claimTextRT != null)
            _claimTextStartPos = claimTextRT.anchoredPosition;

        if (claimTextCG != null)
            claimTextCG.alpha = 1f;

        if (claimTextRT != null)
            claimTextRT.gameObject.SetActive(false);
        if (boostTextRT != null)
        {
            _boostTextStartPos = boostTextRT.anchoredPosition;
            boostTextRT.gameObject.SetActive(false);
        }

        if (boostTextCG != null)
            boostTextCG.alpha = 1f;

    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHeartCapChanged += RefreshHeartCapUI;
            GameManager.Instance.OnLapPreviewChanged += UpdateLapMoneyUI;
            GameManager.Instance.OnLapCompleted += ShowLapCompletedUI;

            UpdateLapMoneyUI(GameManager.Instance.GetLapPreviewMoney());
        }

        if (RoseWallet.Instance != null && roseText != null)
            RoseWallet.Instance.BindRoseText(roseText);

        if (PlayerMoney.Instance != null && moneyText != null)
            PlayerMoney.Instance.BindMoneyText(moneyText);

        if (collectButtonGO != null) collectButtonGO.SetActive(false);
            _nextCheck = 0f;

        Refresh();
        ForceRefreshMergeButton();
        RefreshState();
        InitOfferCycle();
        UpdateBoostOfferUI();
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHeartCapChanged -= RefreshHeartCapUI;
            GameManager.Instance.OnLapPreviewChanged -= UpdateLapMoneyUI;
            GameManager.Instance.OnLapCompleted -= ShowLapCompletedUI;
        }
    }

    void Update()
    {
        UpdateBoostFill();

        bool isAuto = HeartWithEnergy.IsAutoBoostingGlobal;

        // đang auto boost => vẫn hiện countdown + khóa nút
        if (isAuto)
        {
            if (boostAdsBtn != null) boostAdsBtn.interactable = false;

            if (boostTxt != null)
            {
                int s = Mathf.CeilToInt(HeartWithEnergy.GetAutoBoostRemaining());
                boostTxt.text = $"BOOST {s}s";
            }
        }
        else
        {
            // không auto => xoay vòng offer theo 60s
            AdvanceOfferIfNeeded();

            if (boostAdsBtn != null)
                boostAdsBtn.interactable = CanApplyCurrentOffer();

            if (boostTxt != null)
                boostTxt.text = GetCurrentOfferLabel();
        }


        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + checkInterval;

        if (_chain == null) _chain = FindObjectOfType<HeartChainManager>();

        if (Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + checkInterval;

        var bpm = BuildingProductionManager.Instance;
        if (bpm == null || collectButtonGO == null) return;

        bool hasGift = bpm.HasAnyClaimable();

        if (collectButtonGO.activeSelf != hasGift)
        {
            collectButtonGO.SetActive(hasGift);

            if (hasGift)
                StartPulse();
            else
                StopPulse();
        }


        RefreshMergeButtonIfNeeded();

        RefreshGateCostUI();
        RefreshUpgradeRoadUI();
        RefreshFlirtBookUI();  
        RefreshBuildingBookUI();
    }

    // ================= MAIN REFRESH =================

    public void Refresh()
    {
        RefreshGateCostUI();
        RefreshUpgradeRoadUI();
        RefreshFlirtBookUI();  
        RefreshAddHeartIcon();
        RefreshHeartCapUI();
        RefreshCostUI();

        GameManager.Instance?.RefreshLapPreview();
    }

    // ================= ROAD UPGRADE UI =================

    public void RefreshUpgradeRoadUI()
    {
        if (RoadManager.Instance == null)
        {
            if (upgradeRoadBtn != null) upgradeRoadBtn.interactable = false;
            if (upgradeRoadCostText != null) upgradeRoadCostText.text = "-";
            return;
        }

        int unlocked = RoadUpgradeStore.GetUnlockedRoadCount();
        int max = RoadManager.Instance.roadPrefabs != null ? RoadManager.Instance.roadPrefabs.Count : 0;
        bool notMaxed = (max == 0) ? true : (unlocked < max);

        long cost = RoadUpgradeStore.GetNextUpgradeCost();
        long rose = (RoseWallet.Instance != null) ? RoseWallet.Instance.CurrentRose : 0;
        bool hasEnoughRose = rose >= cost;

        int totalGates = (GateManager.Instance != null) ? GateManager.Instance.GatesCount : 0;
        bool hasEnoughGates = totalGates >= RoadManager.Instance.maxGatesPerRoad;

        bool canUpgrade = notMaxed && hasEnoughGates && hasEnoughRose;

        if (upgradeRoadBtn != null)
            upgradeRoadBtn.interactable = canUpgrade;


        if (upgradeRoadCostText != null)
            upgradeRoadCostText.text = cost.ToString("N0"); 

        Debug.Log($"[UI UpgradeRoad] totalGates={totalGates} need={RoadManager.Instance.maxGatesPerRoad} " +
                $"rose={rose} cost={cost} unlocked={unlocked}/{max} can={canUpgrade}");
    }




    // ================= GATE UI =================

    void RefreshGateCostUI()
    {
        // 1) cập nhật progress 2/3, 4/6...
        if (RoadManager.Instance != null)
        {
            int current = RoadManager.Instance.GetTotalGateCountAllRoads();
            int cap = RoadManager.Instance.GetTotalGateCap();

            if (gateProgressText != null)
                gateProgressText.text = string.Format(gateProgressFormat, current, cap);
        }
        else
        {
            if (gateProgressText != null) gateProgressText.text = "0/0";
        }

        // 2) hiển thị cost gate kế tiếp như cũ
        if (gateCostText != null)
        {
            long cost = GateCostStore.GetNextGateCost();
            gateCostText.text = cost.ToString("N0");

            bool enoughRose = (RoseWallet.Instance != null && RoseWallet.Instance.CurrentRose >= cost);

            // rule: add gate chỉ được nếu road hiện tại chưa đủ 3 gate
            bool canAddByRoad = (RoadManager.Instance == null) ? true : RoadManager.Instance.CanAddGateOnCurrentRoad();

            if (addGateBtn != null)
                addGateBtn.interactable = enoughRose && canAddByRoad;
        }
    }


    // ================= COST UI =================

    void RefreshCostUI()
    {
        if (addCostText != null)
        {
            long addCost = ActionCostStore.GetAddCost();
            addCostText.text =  MoneyFormatter.Format(addCost);
        }

        if (mergeCostText != null)
        {
            long mergeCost = ActionCostStore.GetMergeCost();
            mergeCostText.text = MoneyFormatter.Format(mergeCost);
        }
    }

    // ================= MERGE BUTTON =================

    void ForceRefreshMergeButton()
    {
        RefreshMergePreviewUI();
        _lastCanMerge = CanMergeTriple();
    }

    void RefreshMergeButtonIfNeeded()
    {
        bool canMerge = CanMergeTriple();
        if (canMerge == _lastCanMerge) return;

        _lastCanMerge = canMerge;
        RefreshMergePreviewUI();
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
            if (a.type == b.type && b.type == c.type) return true;
        }
        return false;
    }

    void RefreshMergePreviewUI()
    {
        if (HeartManager.Instance == null) return;

        HeartManager.MergePreview p;
        bool can = HeartManager.Instance.TryGetMergePreview(out p);

        if (mergeButtonGO != null) mergeButtonGO.SetActive(can);

        if (mergeCostText != null)
            mergeCostText.gameObject.SetActive(can);

        if (!can)
        {
            if (mergeFromIcon != null) mergeFromIcon.enabled = false;
            if (mergeToIcon != null) mergeToIcon.enabled = false;
            return;
        }

        if (mergeFromIcon != null)
        {
            mergeFromIcon.sprite = p.tripleIcon;
            mergeFromIcon.enabled = (p.tripleIcon != null);
        }

        if (mergeToIcon != null)
        {
            mergeToIcon.sprite = p.resultIcon;
            mergeToIcon.enabled = (p.resultIcon != null);
        }
    }

    // ================= BOOST =================

    public void OnClickUpgradeDrain()
    {
        if (_chain == null) _chain = FindObjectOfType<HeartChainManager>();
        if (_chain == null || _chain.GetLeader() == null) return;

        var energy = _chain.GetLeader().GetComponent<HeartWithEnergy>();
        if (energy == null) return;

        bool success = energy.TryUpgradeDrain();

        if (!success)
        {
            SetMaxState();
            return;
        }

        RefreshState();
        PlayUpgradeTextFX();
    }

    void RefreshState()
    {
        if (_chain == null) _chain = FindObjectOfType<HeartChainManager>();
        if (_chain == null || _chain.GetLeader() == null) return;

        var energy = _chain.GetLeader().GetComponent<HeartWithEnergy>();
        if (energy == null) return;

        if (energy.IsDrainUpgradeMaxed()) SetMaxState();
        else SetNormalState();
    }

    void SetNormalState()
    {
        if (labelText != null) labelText.text = normalText;

        if (boostBtn != null)
        {
            boostBtn.interactable = true;
            // if (_btnImage != null) _btnImage.color = Color.white;
        }
    }

    void SetMaxState()
    {
        if (labelText != null) labelText.text = maxText;

        if (boostBtn != null) boostBtn.interactable = false;
        if (_btnImage != null) _btnImage.color = disabledColor;
    }

    // ================= LAP UI =================

    void UpdateLapMoneyUI(long value)
    {
        if (lapMoneyText == null) return;
        lapMoneyText.text = "+" + MoneyFormatter.Format(value) + "/LAP";
    }

    void ShowLapCompletedUI(long total)
    {
        Debug.Log($"[LAP] Completed. Earned = {total}");
    }

    // ================= ICON / HEART CAP =================

    void RefreshAddHeartIcon()
    {
        if (HeartManager.Instance == null || iconImage == null) return;

        int level = HeartManager.Instance.GetAddableHeartLevel();
        int index = level - 1;

        if (index < 0 || index >= HeartManager.Instance.heartPrefabsByLevel.Count)
        {
            iconImage.enabled = false;
            return;
        }

        GameObject prefab = HeartManager.Instance.heartPrefabsByLevel[index];
        if (prefab == null) { iconImage.enabled = false; return; }

        HeartStats stats = prefab.GetComponent<HeartStats>();
        if (stats == null || stats.icon == null) { iconImage.enabled = false; return; }

        iconImage.sprite = stats.icon;
        iconImage.enabled = true;
    }

    void RefreshHeartCapUI()
    {
        if (heartCapText == null) return;

        if (_chain == null) _chain = FindObjectOfType<HeartChainManager>();

        int current = (_chain != null && _chain.hearts != null) ? _chain.hearts.Count : 0;
        int max = (GameManager.Instance != null) ? GameManager.Instance.MaxHearts : current;

        heartCapText.text = string.Format(capFormat, current, max);
    }

    void RefreshFlirtBookUI()
    {
        if (flirtBookButtonGO == null) return;

        int totalGates = (GateManager.Instance != null) ? GateManager.Instance.GatesCount : 0;

        flirtBookButtonGO.SetActive(totalGates > 0);
    }

    void OnCollectClick()
    {
        var bpm = BuildingProductionManager.Instance;
        if (bpm == null) return;

        if (bpm.TryClaimAllOnCurrentRoad(out var r))
        {
            StopPulse();
            if (collectButtonGO != null) collectButtonGO.SetActive(false);
            ShowClaimText(r.money, r.rose, r.heart, r.boostSeconds);

            Debug.Log($"[COLLECT] money+{r.money} rose+{r.rose} heart+{r.heart} boost+{r.boostSeconds}s");
        }
        else
        {
            if (collectButtonGO != null) collectButtonGO.SetActive(false);
        }
    }

    void StartPulse()
    {
        if (_pulseCR != null) StopCoroutine(_pulseCR);
        _pulseCR = StartCoroutine(PulseCollectButton());
    }

    void StopPulse()
    {
        if (_pulseCR != null)
        {
            StopCoroutine(_pulseCR);
            _pulseCR = null;
        }

        if (collectButtonGO != null)
            collectButtonGO.transform.localScale = Vector3.one;
    }

    IEnumerator PulseCollectButton()
    {
        Transform t = collectButtonGO.transform;
        Vector3 baseScale = Vector3.one;

        while (true)
        {
            float s = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
            float scale = Mathf.Lerp(1f, pulseScale, s);
            t.localScale = baseScale * scale;
            yield return null;
        }
    }



    // ================= BUTTON EVENTS =================

    public void AddHeartBTN()
    {
        int before = HeartChainManager.Instance != null
            ? HeartChainManager.Instance.hearts.Count
            : 0;

        HeartManager.Instance?.AddHeart();

        int after = HeartChainManager.Instance != null
            ? HeartChainManager.Instance.hearts.Count
            : before;

        bool ok = after > before;

        if (ok && roseFlyFX != null)
            roseFlyFX.Play(addBtnRT, roseTargetRT);

        Refresh();
        ForceRefreshMergeButton();
    }

    public void MergeHeartBTN()
    {
        HeartManager.Instance.MergeAnyTriple();
        roseFlyFX.Play(addBtnRT, roseTargetRT);
        Refresh();
        ForceRefreshMergeButton();
    }

    public void AddGateBTN()
    {
        bool ok = (GateManager.Instance != null) && GateManager.Instance.SpawnGate();
        RefreshGateCostUI();
        RefreshFlirtBookUI();  

        if (ok)
            GameManager.Instance?.RefreshLapPreview();


        Refresh();
    }

    public void UpgradeRoadBTN()
    {
        Debug.Log("[UI] UpgradeRoadBTN clicked");

        if (RoadManager.Instance == null)
        {
            Debug.LogWarning("[UI] RoadManager.Instance is null");
            return;
        }

        bool ok = RoadManager.Instance.TryUpgradeRoad();
        Debug.Log($"[UI] TryUpgradeRoad result = {ok}");

        RefreshUpgradeRoadUI();
        RefreshGateCostUI();

        if (ok)
            GameManager.Instance?.RefreshLapPreview();
    }


    public void NextRoadBTN()
    {
        RoadManager.Instance?.NextRoad();
        RefreshGateCostUI();
        RefreshUpgradeRoadUI();
        RefreshBuildingBookUI();
        GameManager.Instance?.RefreshLapPreview();
    }

    public void OpenFlirtBookBTN()
    {
        UIManager.Instance.OpenUI<FlirtBookPanel>();
    }
    
    public void OpenSettingBTN()
    {
        UIManager.Instance.OpenUI<PanelSetting>();
    }

    public void Boost60sBTN()
    {
        if (HeartWithEnergy.IsAutoBoostingGlobal) return;

        string msg = GetBoostOfferPopupMessage();

        ApplyCurrentOffer();

        PlayBoostTextFX(msg);

        AdvanceOfferImmediate();
        SwitchToNextOfferNow();
    }



    public void ReviewBTN()
    {
        UIManager.Instance.OpenUI<PanelReview>();
    }

    public void BuildingBookBTn()
    {
        UIManager.Instance.OpenUI<PanelBuilding>();
    }

    void RefreshBuildingBookUI()
    {
        if (buildingBookButtonGO == null)
            return;

        if (RoadManager.Instance == null)
        {
            buildingBookButtonGO.SetActive(false);
            return;
        }

        int roadIndex = RoadManager.Instance.CurrentRoadIndex;

        buildingBookButtonGO.SetActive(roadIndex >= 1);
    }

    void InitOfferCycle()
    {
        _offerIndex = 0;
        _offerStartTime = Time.unscaledTime;
        _nextOfferSwitchTime = _offerStartTime + offerSwitchInterval;
    }


    void AdvanceOfferIfNeeded()
    {
        if (Time.unscaledTime < _nextOfferSwitchTime) return;

        _offerIndex = (_offerIndex + 1) % offerCycle.Length;

        _offerStartTime = Time.unscaledTime;
        _nextOfferSwitchTime = _offerStartTime + offerSwitchInterval;

        UpdateBoostOfferUI();
    }


    BoostOffer CurrentOffer
    {
        get
        {
            if (offerCycle == null || offerCycle.Length == 0) return BoostOffer.Boost60s;
            int idx = Mathf.Clamp(_offerIndex, 0, offerCycle.Length - 1);
            return offerCycle[idx];
        }
    }

    string GetCurrentOfferLabel()
    {
        switch (CurrentOffer)
        {
            case BoostOffer.Boost60s: return "+60s Boost";
            case BoostOffer.Rose20:   return "+20";
            case BoostOffer.Heart10:  return "+10";
            default:                  return "+60s Boost";
        }
    }

    void UpdateBoostOfferUI()
    {
        if (boostTxt == null) return;

        // ================= AUTO BOOST =================
        if (HeartWithEnergy.IsAutoBoostingGlobal)
        {
            int s = Mathf.CeilToInt(HeartWithEnergy.GetAutoBoostRemaining());
            boostTxt.text = $"Boost {s}s";

            if (offerIconImage != null)
                offerIconImage.sprite = boostSprite;

            if (roseIconImage != null) roseIconImage.gameObject.SetActive(false);
            if (heartIconImage != null) heartIconImage.gameObject.SetActive(false);

            if (boostAdsBtn != null)
                boostAdsBtn.interactable = false;

            return;
        }

        // ================= NORMAL OFFER =================

        if (roseIconImage != null) roseIconImage.gameObject.SetActive(false);
        if (heartIconImage != null) heartIconImage.gameObject.SetActive(false);


        switch (CurrentOffer)
        {
            case BoostOffer.Boost60s:
                boostTxt.text = "+60s Boost";
                break;

            case BoostOffer.Rose20:
                boostTxt.text = "+20";
                if (roseIconImage != null)
                    roseIconImage.gameObject.SetActive(true);
                break;

            case BoostOffer.Heart10:
                boostTxt.text = "+10";
                if (heartIconImage != null)
                    heartIconImage.gameObject.SetActive(true);
                break;
        }

        if (offerIconImage != null)
            offerIconImage.sprite = GetOfferSprite(CurrentOffer);

        if (boostAdsBtn != null)
            boostAdsBtn.interactable = CanApplyCurrentOffer();
    }





    bool CanApplyCurrentOffer()
    {
        switch (CurrentOffer)
        {
            case BoostOffer.Boost60s:
                return true;

            case BoostOffer.Rose20:
                return RoseWallet.Instance != null;

            case BoostOffer.Heart10:
                return CanAddAnyHeart();

            default:
                return true;
        }
    }

    bool CanAddAnyHeart()
    {
        int cur = (HeartChainManager.Instance != null && HeartChainManager.Instance.hearts != null)
            ? HeartChainManager.Instance.hearts.Count
            : 0;

        int max = (GameManager.Instance != null) ? GameManager.Instance.MaxHearts : cur;
        return cur < max;
    }

    void ApplyCurrentOffer()
    {
        switch (CurrentOffer)
        {
            case BoostOffer.Boost60s:
                HeartWithEnergy.StartAutoBoost(60f);
                break;

            case BoostOffer.Rose20:
                RoseWallet.Instance?.AddRose(20);
                break;

            case BoostOffer.Heart10:
                AddHeartsUpTo(10);
                break;
        }

        _offerStartTime = Time.unscaledTime;
        _nextOfferSwitchTime = _offerStartTime + offerSwitchInterval;
    }

    void AddHeartsUpTo(int count)
    {
        if (HeartManager.Instance == null) return;

        int added = 0;

        int before = (HeartChainManager.Instance != null && HeartChainManager.Instance.hearts != null)
            ? HeartChainManager.Instance.hearts.Count
            : 0;

        for (int i = 0; i < count; i++)
        {
            if (!CanAddAnyHeart()) break;

            int b = (HeartChainManager.Instance != null && HeartChainManager.Instance.hearts != null)
                ? HeartChainManager.Instance.hearts.Count
                : 0;

            HeartManager.Instance.AddHeart();

            int a = (HeartChainManager.Instance != null && HeartChainManager.Instance.hearts != null)
                ? HeartChainManager.Instance.hearts.Count
                : b;

            if (a > b) added++;
            else break;
        }

        if (added > 0)
        {
            RefreshHeartCapUI();
            RefreshAddHeartIcon();
            ForceRefreshMergeButton();
            GameManager.Instance?.RefreshLapPreview();
        }
    }

    void UpdateBoostFill()
    {
        if (boostFillImage == null) return;

        // Khi đang auto boost → fill full
        if (HeartWithEnergy.IsAutoBoostingGlobal)
        {
            boostFillImage.fillAmount = 1f;
            return;
        }

        float elapsed = Time.unscaledTime - _offerStartTime;
        float t = Mathf.Clamp01(elapsed / offerSwitchInterval);

        // Fill giảm dần
        boostFillImage.fillAmount = 1f - t;
    }

    Sprite GetOfferSprite(BoostOffer offer)
    {
        switch (offer)
        {
            case BoostOffer.Boost60s: return boostSprite;
            case BoostOffer.Rose20:   return roseSprite;
            case BoostOffer.Heart10:  return heartSprite;
            default: return boostSprite;
        }
    }
    void SwitchToNextOfferNow()
    {
        if (offerCycle == null || offerCycle.Length == 0) return;

        _offerIndex = (_offerIndex + 1) % offerCycle.Length;

        _offerStartTime = Time.unscaledTime;
        _nextOfferSwitchTime = _offerStartTime + offerSwitchInterval;

        UpdateBoostOfferUI();     
        UpdateBoostFill();        
    }

    void AdvanceOfferImmediate()
    {
        _offerIndex = (_offerIndex + 1) % offerCycle.Length;
        _offerStartTime = Time.unscaledTime;
        _nextOfferSwitchTime = _offerStartTime + offerSwitchInterval;

        UpdateBoostOfferUI();
    }

    void PlayUpgradeTextFX()
    {
        if (upgradeTextRT == null || upgradeTextCG == null)
            return;

        if (_upgradeTextCR != null)
            StopCoroutine(_upgradeTextCR);

        _upgradeTextCR = StartCoroutine(UpgradeTextFX_CR());
    }

    IEnumerator UpgradeTextFX_CR()
    {
        upgradeTextRT.gameObject.SetActive(true);
        upgradeTextRT.anchoredPosition = _upgradeTextStartPos;
        upgradeTextCG.alpha = 1f;

        float t = 0f;

        while (t < upgradeTextDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / upgradeTextDuration);

            // Move up
            upgradeTextRT.anchoredPosition =
                _upgradeTextStartPos + Vector2.up * Mathf.Lerp(0f, upgradeTextMoveY, p);

            // Fade out
            upgradeTextCG.alpha = 1f - p;

            yield return null;
        }

        // Reset
        upgradeTextRT.gameObject.SetActive(false);
        upgradeTextRT.anchoredPosition = _upgradeTextStartPos;
        upgradeTextCG.alpha = 1f;
    }

    void ShowClaimText(long money, int rose, int heart, float boostSeconds)
    {
        if (claimText == null) return;

        var sb = new System.Text.StringBuilder();

        if (rose > 0) sb.AppendLine($"+ {rose} rose");
        if (money > 0) sb.AppendLine($"+ {MoneyFormatter.Format(money)} $");
        if (heart > 0) sb.AppendLine($"+ {heart} heart");
        if (boostSeconds > 0) sb.AppendLine($"+ {Mathf.RoundToInt(boostSeconds)}s boost");

        if (sb.Length == 0) return;

        claimText.text = sb.ToString().TrimEnd();
        PlayClaimTextFX();
    }

    void PlayClaimTextFX()
    {
        if (claimTextRT == null || claimTextCG == null) return;

        if (_claimTextCR != null)
            StopCoroutine(_claimTextCR);

        _claimTextCR = StartCoroutine(ClaimTextFX_CR());
    }

    IEnumerator ClaimTextFX_CR()
    {
        claimTextRT.gameObject.SetActive(true);
        claimTextRT.anchoredPosition = _claimTextStartPos;
        claimTextCG.alpha = 1f;

        float t = 0f;

        while (t < claimTextDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / claimTextDuration);

            claimTextRT.anchoredPosition =
                _claimTextStartPos + Vector2.up * Mathf.Lerp(0f, claimTextMoveY, p);

            claimTextCG.alpha = 1f - p;

            yield return null;
        }

        claimTextRT.gameObject.SetActive(false);
        claimTextRT.anchoredPosition = _claimTextStartPos;
        claimTextCG.alpha = 1f;
        _claimTextCR = null;
    }

    void PlayBoostTextFX(string msg)
    {
        if (boostTextRT == null || boostTextCG == null || boostText == null) return;

        boostText.text = msg;

        if (_boostTextCR != null)
            StopCoroutine(_boostTextCR);

        _boostTextCR = StartCoroutine(BoostTextFX_CR());
    }

    IEnumerator BoostTextFX_CR()
    {
        boostTextRT.gameObject.SetActive(true);
        boostTextRT.anchoredPosition = _boostTextStartPos;
        boostTextCG.alpha = 1f;

        float t = 0f;

        while (t < boostTextDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / boostTextDuration);

            boostTextRT.anchoredPosition =
                _boostTextStartPos + Vector2.up * Mathf.Lerp(0f, boostTextMoveY, p);

            boostTextCG.alpha = 1f - p;

            yield return null;
        }

        boostTextRT.gameObject.SetActive(false);
        boostTextRT.anchoredPosition = _boostTextStartPos;
        boostTextCG.alpha = 1f;
    }

    string GetBoostOfferPopupMessage()
    {
        if (HeartWithEnergy.IsAutoBoostingGlobal)
        {
            int s = Mathf.CeilToInt(HeartWithEnergy.GetAutoBoostRemaining());
            return $"Boost {s}s";
        }

        switch (CurrentOffer)
        {
            case BoostOffer.Boost60s: return "Boost 60s";
            case BoostOffer.Rose20:   return "+ 20 rose";
            case BoostOffer.Heart10:  return "+ 10 heart";
            default:                  return "Boost";
        }
    }



}
