using UnityEngine;
using UnityEngine.UI;

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

    float _currentEnergy;
    float _currentSpeed;
    float _targetSpeed;
    float _lastPressTime;
    float _distanceOnPath;

    Image _energyImage;
    CanvasGroup _canvasGroup;
    Camera _mainCam;

    void Awake()
    {
        _mainCam = Camera.main;
        CacheUIRefs();
    }

    void Start()
    {
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

        bool isPressing = IsPressing();
        if (isPressing) _lastPressTime = Time.time;

        HandleEnergyAndSpeed(isPressing);

        bool isBoosting = isPressing && _currentEnergy > 0f;
        IsBoostingGlobal = isBoosting;

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
        HandleFade(isPressing);
    }

    void OnDisable()
    {
        if (IsBoostingGlobal) IsBoostingGlobal = false;
    }

    bool IsPressing()
    {
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
}
