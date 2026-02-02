using UnityEngine;
using System;
using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

#region BOOTSTRAP
public static class AdServiceBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (GameObject.Find("AdServiceRuntime") != null) return;

        var go = new GameObject("AdServiceRuntime");
        UnityEngine.Object.DontDestroyOnLoad(go);

        go.AddComponent<InterstitialAdManager>();
        go.AddComponent<RewardedAdManager>();
        go.AddComponent<AdServiceRuntime>();

        Debug.Log("[AdServiceBootstrap] Runtime initialized.");
    }
}
#endregion

#region RUNTIME SERVICE CONTAINER
public class AdServiceRuntime : MonoBehaviour
{
    public static AdServiceRuntime Instance;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
#endregion

/*-------------------------------------------------------------
 *  INTERSTITIAL MANAGER
 *-------------------------------------------------------------*/
#region INTERSTITIAL MANAGER
public class InterstitialAdManager : MonoBehaviour
{
    public static InterstitialAdManager Instance;

#if UNITY_ANDROID
    [SerializeField] private string adUnitId = "ca-app-pub-1650520002983936/8091183282";
#elif UNITY_IOS
    [SerializeField] private string adUnitId = "";
        // [SerializeField] private string adUnitId = "ca-app-pub-9960915674223286/9296050485";

#else
    [SerializeField] private string adUnitId = "unused";
#endif

    private InterstitialAd _ad;
    private Action _onClosed;

    private int _retries = 0;
    private const int MAX_RETRIES = 3;
    private const float BASE_DELAY = 2f;
    private const float MAX_DELAY = 30f;

    private DateTime _lastShown = DateTime.MinValue;
    private readonly TimeSpan COOLDOWN = TimeSpan.FromSeconds(45);

    private const float RELOAD_AFTER_CLOSE = 45f;
private const float INITIAL_LOAD_DELAY = 60f;
private bool _allowLoad = false;
private bool _allowShow = false;
    // void Awake()
    // {
    //     if (Instance == null)
    //     {
    //         Instance = this;
    //         DontDestroyOnLoad(gameObject);
    //         LoadAd();
    //     }
    //     else Destroy(gameObject);
    // }
void Awake()
{
    if (Instance != null)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);

    // ⏳ Delay load lần đầu
    StartCoroutine(InitialLoadDelayed());
}
private IEnumerator InitialLoadDelayed()
{
    yield return new WaitForSeconds(INITIAL_LOAD_DELAY);
    _allowLoad = true;
    _allowShow = true;
    LoadAd();
}
    public bool IsReady() =>
        _ad != null && _ad.CanShowAd();
public void Show(Action onClosed)
{
    if (!_allowShow)
    {
        onClosed?.Invoke();
        return;
    }

    if (DateTime.Now - _lastShown < COOLDOWN)
    {
        onClosed?.Invoke();
        return;
    }

    if (IsReady())
    {
        _onClosed = onClosed;
        _ad.Show();
    }
    else
    {
        onClosed?.Invoke();
        // ❌ KHÔNG LoadAd() ở đây
    }
}

    // public void Show(Action onClosed)


    // {
    //     if (DateTime.Now - _lastShown < COOLDOWN)
    //     {
    //         Debug.Log("[Interstitial] Cooldown active -> no show");
    //         onClosed?.Invoke();
    //         return;
    //     }

    //     if (IsReady())
    //     {
    //         _onClosed = onClosed;
    //         _ad.Show();
    //     }
    //     else
    //     {
    //         Debug.Log("[Interstitial] Not ready");
    //         onClosed?.Invoke();
    //         LoadAd();
    //     }
    // }

    // public void LoadAd()
    // {
        
    //     if (_ad != null)
    //     {
    //         _ad.Destroy();
    //         _ad = null;
    //     }

    //     var request = new AdRequest();
    //     InterstitialAd.Load(adUnitId, request, (ad, error) =>
    //     {
    //         if (error != null || ad == null)
    //         {
    //             Debug.LogWarning("[Interstitial] Load failed: " + error);

    //             if (_retries < MAX_RETRIES)
    //             {
    //                 _retries++;
    //                 float delay = Mathf.Min(MAX_DELAY, BASE_DELAY * Mathf.Pow(2, _retries - 1));
    //                 StartCoroutine(LoadDelayed(delay));
    //                 return;
    //             }

    //             Debug.LogWarning("[Interstitial] Retry limit reached.");
    //             return;
    //         }

    //         _ad = ad;
    //         _retries = 0;

    //         ad.OnAdFullScreenContentClosed += OnClosed;
    //         ad.OnAdFullScreenContentFailed += _ =>
    //         {
    //             _onClosed?.Invoke();
    //             LoadAd();
    //         };

    //         Debug.Log("[Interstitial] Loaded.");
    //     });
    // }


public void LoadAd()
{
    // ⛔ Chưa tới thời điểm cho phép load (delay lần đầu)
    if (!_allowLoad)
    {
        Debug.Log("[Interstitial] Load blocked - initial delay");
        return;
    }

    // ⛔ Tránh load chồng
    if (_ad != null)
    {
        Debug.Log("[Interstitial] Already has ad");
        return;
    }

    var request = new AdRequest();

    InterstitialAd.Load(adUnitId, request, (ad, error) =>
    {
        if (error != null || ad == null)
        {
            Debug.LogWarning("[Interstitial] Load failed: " + error);

            // Retry có delay
            if (_retries < MAX_RETRIES)
            {
                _retries++;
                float delay = Mathf.Min(
                    MAX_DELAY,
                    BASE_DELAY * Mathf.Pow(2, _retries - 1)
                );
                StartCoroutine(LoadDelayed(delay));
            }
            return;
        }

        // ✅ Load thành công
        _ad = ad;
        _retries = 0;

        ad.OnAdFullScreenContentClosed += OnClosed;
        ad.OnAdFullScreenContentFailed += _ =>
        {
            _onClosed?.Invoke();
            _onClosed = null;

            _ad?.Destroy();
            _ad = null;

            // Load lại theo cooldown 45s
            StartCoroutine(LoadDelayed(RELOAD_AFTER_CLOSE));
        };

        Debug.Log("[Interstitial] Loaded.");
    });
}



    private void OnClosed()
    {
        _lastShown = DateTime.Now;

        _onClosed?.Invoke();
        _onClosed = null;

        _ad?.Destroy();
        _ad = null;

        StartCoroutine(LoadDelayed(RELOAD_AFTER_CLOSE));
    }

    private IEnumerator LoadDelayed(float sec)
    {
        yield return new WaitForSeconds(sec);
        LoadAd();
    }
}
#endregion

/*-------------------------------------------------------------
 *  REWARDED MANAGER
 *-------------------------------------------------------------*/
#region REWARDED MANAGER
public class RewardedAdManager : MonoBehaviour
{
    public static RewardedAdManager Instance;

#if UNITY_ANDROID
    [SerializeField] private string adUnitId = "ca-app-pub-1650520002983936/4647789961";
#elif UNITY_IOS
    [SerializeField] private string adUnitId = "";
        // [SerializeField] private string adUnitId = "ca-app-pub-9960915674223286/1600451341";

#else
    [SerializeField] private string adUnitId = "unused";
#endif

    private RewardedAd _ad;
    private Action _onEarned;
    private Action _onClosed;

    private int _retries = 0;
    private const int MAX_RETRIES = 3;
    private const float BASE_DELAY = 2f;
    private const float MAX_DELAY = 30f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadAd();
        }
        else Destroy(gameObject);
    }

    public bool IsReady() =>
        _ad != null && _ad.CanShowAd();

    public void Show(Action onEarned, Action onClosed)
    {
        if (!IsReady())
        {
            Debug.Log("[Rewarded] Not ready");
            onClosed?.Invoke();
            LoadAd();
            return;
        }

        _onEarned = onEarned;
        _onClosed = onClosed;

        _ad.Show(reward =>
        {
            _onEarned?.Invoke();
        });
    }

    public void LoadAd()
    {
        if (_ad != null)
        {
            _ad.Destroy();
            _ad = null;
        }

        RewardedAd.Load(adUnitId, new AdRequest(), (ad, err) =>
        {
            if (err != null || ad == null)
            {
                Debug.LogWarning("[Rewarded] Load failed: " + err);

                if (_retries < MAX_RETRIES)
                {
                    _retries++;
                    float delay = Mathf.Min(MAX_DELAY, BASE_DELAY * Mathf.Pow(2, _retries - 1));
                    StartCoroutine(LoadDelayed(delay));
                    return;
                }

                Debug.LogWarning("[Rewarded] Retry limit reached.");
                return;
            }

            _ad = ad;
            _retries = 0;

            ad.OnAdFullScreenContentClosed += OnClosed;
            ad.OnAdFullScreenContentFailed += _ =>
            {
                _onClosed?.Invoke();
                LoadAd();
            };

            Debug.Log("[Rewarded] Loaded.");
        });
    }

    private void OnClosed()
    {
        _onClosed?.Invoke();
        _onClosed = null;
        _onEarned = null;

        _ad?.Destroy();
        _ad = null;

        StartCoroutine(LoadDelayed(0f));
    }

    private IEnumerator LoadDelayed(float sec)
    {
        yield return new WaitForSeconds(sec);
        LoadAd();
    }
}
#endregion

/*-------------------------------------------------------------
 *  PUBLIC API FOR GAMEPLAY
 *-------------------------------------------------------------*/
#region AD SERVICE API
public static class AdService
{
    public static void ShowInterstitial(Action onClosed = null)
        => InterstitialAdManager.Instance?.Show(onClosed);

    public static bool IsInterstitialReady()
        => InterstitialAdManager.Instance != null &&
           InterstitialAdManager.Instance.IsReady();

    public static void ShowRewarded(Action onEarned, Action onClosed = null)
        => RewardedAdManager.Instance?.Show(onEarned, onClosed);

    public static bool IsRewardedReady()
        => RewardedAdManager.Instance != null &&
           RewardedAdManager.Instance.IsReady();
}
#endregion
