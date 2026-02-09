using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class HeartWithEnergy : MonoBehaviour
{
    public static bool IsBoostingGlobal { get; private set; }

    [Header("Movement")]
    public bool driveMovement = false;

    [Header("Path / Center refs (optional)")]
    public SplinePath path;
    public Transform center;

    [Header("Speed")]
    public float normalSpeed = 30f;
    public float boostSpeed = 100f;
    public float speedLerp = 5f;

    [Header("Energy Bar UI (SHARED)")]
    public RectTransform energyBar;
    public Transform barRoot;
    public Vector3 barWorldOffset = new Vector3(0, -1.5f, 0);

    [Header("Energy Settings")]
    public float maxEnergy = 100f;
    public float drainPerSecond = 50f;
    public float refillPerSecond = 40f;

    [Header("Fade")]
    public float fadeDelay = 20f;
    public float fadeSpeed = 2f;
    [Range(0f, 1f)] public float fadedAlpha = 0f;

    [Header("Boost VFX")]
    public ParticleSystem boostVFX;
    public bool clearOnStop = true;

    [Header("Drain Upgrade")]
    public int maxUpgradeCount = 10;
    public float reducePerUpgrade = 2.5f;
    public float minDrainPerSecond = 5f;

    const string PP_DRAIN_UPGRADE = "ENERGY_DRAIN_UPGRADE_COUNT";

    int _upgradeCount;
    float _baseDrainPerSecond;

    float _currentEnergy;
    float _currentSpeed;
    float _targetSpeed;
    float _lastPressTime;
    float _distanceOnPath;

    Image _energyImage;
    CanvasGroup _canvasGroup;
    Camera _mainCam;

    static readonly List<RaycastResult> _uiHits = new();
    PointerEventData _ped;

    public static bool IsAutoBoostingGlobal { get; private set; }

    private static float _autoBoostEndTime;

    public static float AutoBoostEndTime { get; private set; }

    public static bool CanManualPress => !IsAutoBoostingGlobal;

    private static bool _lastBoostingGlobal;


    void Awake()
    {
        _mainCam = Camera.main;
        CacheUIRefs();
    }

    void Start()
    {
        _baseDrainPerSecond = drainPerSecond;

        _upgradeCount = PlayerPrefs.GetInt(PP_DRAIN_UPGRADE, 0);

        drainPerSecond = Mathf.Max(
            minDrainPerSecond,
            _baseDrainPerSecond - _upgradeCount * reducePerUpgrade
        );

        _currentEnergy = maxEnergy;
        _currentSpeed = normalSpeed;
        _targetSpeed = normalSpeed;
        _lastPressTime = Time.time;

        if (_energyImage != null) _energyImage.fillAmount = 1f;
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
    }


    public void BindUI(RectTransform sharedBar, Transform sharedRoot, Transform sharedCenter)
    {
        energyBar = sharedBar;
        barRoot = sharedRoot;
        if (center == null) center = sharedCenter;

        CacheUIRefs();

        if (_energyImage != null)
            _energyImage.fillAmount = Mathf.Clamp01(_currentEnergy / maxEnergy);

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        FollowSelfForEnergyBar();
    }

    void CacheUIRefs()
    {
        if (energyBar != null) _energyImage = energyBar.GetComponent<Image>();

        if (barRoot != null)
        {
            _canvasGroup = barRoot.GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = barRoot.gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Update()
    {
        FollowSelfForEnergyBar();

        if (IsAutoBoostingGlobal && Time.time >= AutoBoostEndTime)
            IsAutoBoostingGlobal = false;

        bool manualPress = !IsAutoBoostingGlobal && IsPressing();
        bool autoBoost = IsAutoBoostingGlobal;

        bool boostingInput = autoBoost || manualPress;
        if (boostingInput) _lastPressTime = Time.time;

        if (autoBoost)
        {
            _targetSpeed = boostSpeed;
            if (_currentEnergy < maxEnergy)
                _currentEnergy = Mathf.Min(maxEnergy, _currentEnergy + refillPerSecond * Time.deltaTime);
        }
        else
        {
            HandleEnergyAndSpeed(manualPress);
        }

        bool isBoosting = boostingInput && (autoBoost || _currentEnergy > 0f);
        IsBoostingGlobal = isBoosting;

        if (IsBoostingGlobal != _lastBoostingGlobal)
        {
            _lastBoostingGlobal = IsBoostingGlobal;
            SetAllHeartBoostWind(IsBoostingGlobal);
        }

        UpdateBoostVFX(isBoosting);


        _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, speedLerp * Time.deltaTime);

        if (driveMovement)
        {
            if (path != null && path.TotalLength > 0f)
            {
                _distanceOnPath += _currentSpeed * Time.deltaTime;
                path.SampleAtDistance(_distanceOnPath, out var pos, out var fwd);
                transform.position = pos;
                transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
            else if (center != null)
            {
                transform.RotateAround(center.position, Vector3.down, _currentSpeed * Time.deltaTime);
            }
        }

        UpdateEnergyUI();
        HandleFade(boostingInput);
        }

        bool IsPointerBlockingByUI()
        {
            if (EventSystem.current == null) return false;

            // Mobile: ưu tiên touch position để chính xác
            Vector2 pos;
    #if UNITY_EDITOR || UNITY_STANDALONE
            pos = Input.mousePosition;
    #else
            if (Input.touchCount == 0) return false;
            pos = Input.GetTouch(0).position;
    #endif

            if (_ped == null) _ped = new PointerEventData(EventSystem.current);
            _ped.Reset();
            _ped.position = pos;

            _uiHits.Clear();
            EventSystem.current.RaycastAll(_ped, _uiHits);

            return _uiHits.Count > 0;
        }


    void OnDisable()
    {
        if (IsBoostingGlobal)
        {
            IsBoostingGlobal = false;
            _lastBoostingGlobal = false;
            SetAllHeartBoostWind(false);
        }
    }

    bool IsPressing()
    {
        if (IsAutoBoostingGlobal) return false;

        if (IsPointerBlockingByUI())
            return false;
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.GetMouseButton(0);
#else
        return Input.touchCount > 0;
#endif
    }

    void HandleEnergyAndSpeed(bool isPressing)
    {
        _targetSpeed = normalSpeed;

        if (isPressing && _currentEnergy > 0f)
        {
            _targetSpeed = boostSpeed;
            _currentEnergy = Mathf.Max(0f, _currentEnergy - drainPerSecond * Time.deltaTime);
        }
        else if (!isPressing && _currentEnergy < maxEnergy)
        {
            _currentEnergy = Mathf.Min(maxEnergy, _currentEnergy + refillPerSecond * Time.deltaTime);
        }
    }

    void UpdateEnergyUI()
    {
        if (_energyImage == null) return;
        _energyImage.fillAmount = Mathf.Clamp01(_currentEnergy / maxEnergy);
    }

    void HandleFade(bool isPressing)
    {
        if (_canvasGroup == null) return;

        float targetAlpha = 1f;

        if (_currentEnergy >= maxEnergy - 0.01f && !isPressing)
        {
            float idleTime = Time.time - _lastPressTime;
            if (idleTime >= fadeDelay) targetAlpha = fadedAlpha;
        }

        _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
    }

    void UpdateBoostVFX(bool isBoosting)
    {
        if (boostVFX == null) return;

        if (isBoosting)
        {
            if (!boostVFX.isPlaying) boostVFX.Play();
        }
        else
        {
            if (boostVFX.isPlaying)
            {
                boostVFX.Stop(true, clearOnStop
                    ? ParticleSystemStopBehavior.StopEmittingAndClear
                    : ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    void FollowSelfForEnergyBar()
    {
        if (_mainCam == null) _mainCam = Camera.main;
        if (_mainCam == null) return;

        var target = barRoot != null ? barRoot : (Transform)energyBar;
        if (target == null) return;

        Vector3 worldPos = transform.position + barWorldOffset;
        Vector3 screenPos = _mainCam.WorldToScreenPoint(worldPos);

        target.position = screenPos;

        if (energyBar != null && (Transform)energyBar != target)
            energyBar.position = screenPos;
    }

    public bool TryUpgradeDrain()
    {
        if (_upgradeCount >= maxUpgradeCount)
            return false;

        _upgradeCount++;

        drainPerSecond = Mathf.Max(
            minDrainPerSecond,
            _baseDrainPerSecond - _upgradeCount * reducePerUpgrade
        );

        PlayerPrefs.SetInt(PP_DRAIN_UPGRADE, _upgradeCount);
        PlayerPrefs.Save();

        Debug.Log($"[EnergyUpgrade] Lv {_upgradeCount}/{maxUpgradeCount} - Drain={drainPerSecond}");
        return true;
    }

    public bool IsDrainUpgradeMaxed()
    {
        return _upgradeCount >= maxUpgradeCount;
    }

    public int GetUpgradeCount()
    {
        return _upgradeCount;
    }

    public static void StartAutoBoost(float durationSeconds)
    {
        if (durationSeconds <= 0f) return;

        IsAutoBoostingGlobal = true;
        AutoBoostEndTime = Time.time + durationSeconds;
        
    }

    public static float GetAutoBoostRemaining()
    {
        if (!IsAutoBoostingGlobal) return 0f;
        return Mathf.Max(0f, AutoBoostEndTime - Time.time);
    }

    public static void StopAutoBoost()
    {
        IsAutoBoostingGlobal = false;
        _autoBoostEndTime = 0f;

        
    }

    static void SetAllHeartBoostWind(bool on)
    {
        if (HeartChainManager.Instance == null) return;

        foreach (var heart in HeartChainManager.Instance.hearts)
        {
            if (!heart) continue;
            heart.GetComponent<HeartBoostWind>()?.SetBoosting(on);
        }
    }


}
