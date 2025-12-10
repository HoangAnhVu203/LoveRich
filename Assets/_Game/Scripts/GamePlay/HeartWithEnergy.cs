using UnityEngine;
using UnityEngine.UI;


[DisallowMultipleComponent]
public class HeartWithEnergy : MonoBehaviour
{
    public static bool IsBoostingGlobal { get; private set; }

    [Header("Rotation Settings")]
    public Transform center;           
    public float normalSpeed = 30f;    
    public float boostSpeed = 100f;    
    public float speedLerp = 5f;      

    [Header("Energy Bar UI")]
    public RectTransform energyBar;    
    public Transform barRoot;          
    public Vector3 barWorldOffset = new Vector3(0, -1.5f, 0); 

    [Header("Energy Settings")]
    public float maxEnergy = 100f;
    public float drainPerSecond = 50f;   
    public float refillPerSecond = 40f;  

    [Header("Blur")]
    public float fadeDelay = 20f;       
    public float fadeSpeed = 2f;        
    [Range(0f, 1f)]
    public float fadedAlpha = 0f;      

    [Header("Boost VFX (cho heart này)")]
    public ParticleSystem boostVFX;      
    public bool clearOnStop = true;     

    float _currentEnergy;
    float _currentSpeed;
    float _targetSpeed;
    float _lastPressTime;

    Image _energyImage;          
    CanvasGroup _canvasGroup;    
    Camera _mainCam;

    void Awake()
    {
        _mainCam = Camera.main;

        if (energyBar != null)
            _energyImage = energyBar.GetComponent<Image>();

        if (barRoot != null)
        {
            _canvasGroup = barRoot.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = barRoot.gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Start()
    {
        _currentEnergy = maxEnergy;
        _currentSpeed = normalSpeed;
        _targetSpeed = normalSpeed;

        if (_energyImage != null)
            _energyImage.fillAmount = 1f;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        _lastPressTime = Time.time;
    }

    void Update()
    {
        FollowSelfForEnergyBar();

        bool isPressing = IsPressing();
        if (isPressing)
        {
            _lastPressTime = Time.time;
        }

        HandleEnergyAndSpeed(isPressing);

        bool isBoosting = isPressing && _currentEnergy > 0f;
        IsBoostingGlobal = isBoosting;

        UpdateBoostVFX(isBoosting);

        _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, speedLerp * Time.deltaTime);

        if (center != null)
        {
            transform.RotateAround(center.position, Vector3.up, _currentSpeed * Time.deltaTime);
        }

        UpdateEnergyUI();
        HandleFade(isPressing);
    }

    void OnDisable()
    {
        IsBoostingGlobal = false;
    }

    void FollowSelfForEnergyBar()
    {
        if (energyBar == null || _mainCam == null) return;

        Vector3 worldPos = transform.position + barWorldOffset;
        Vector3 screenPos = _mainCam.WorldToScreenPoint(worldPos);

        energyBar.position = screenPos;
        if (barRoot != null)
            barRoot.position = screenPos;
    }

    bool IsPressing()
    {
        if (InputHelper.IsPointerOverUI())
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
            _currentEnergy -= drainPerSecond * Time.deltaTime;
            _currentEnergy = Mathf.Max(0f, _currentEnergy);
        }

        if (!isPressing && _currentEnergy < maxEnergy)
        {
            _currentEnergy += refillPerSecond * Time.deltaTime;
            _currentEnergy = Mathf.Min(maxEnergy, _currentEnergy);
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
            if (idleTime >= fadeDelay)
                targetAlpha = fadedAlpha;
        }

        _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
    }

    void UpdateBoostVFX(bool isBoosting)
    {
        if (boostVFX == null) return;

        if (isBoosting)
        {
            if (!boostVFX.isPlaying)
                boostVFX.Play();
        }
        else
        {
            if (boostVFX.isPlaying)
            {
                if (clearOnStop)
                    boostVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                else
                    boostVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}
