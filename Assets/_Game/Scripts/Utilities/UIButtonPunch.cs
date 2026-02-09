using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIButtonPunch : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] RectTransform target; // null = chính button

    [Header("Scale Bounce")]
    [SerializeField] float scaleUp = 1.12f;     // phồng to bao nhiêu
    [SerializeField] float upTime = 0.08f;      // thời gian phồng
    [SerializeField] float downTime = 0.12f;    // thời gian thu về
    [SerializeField] bool useUnscaledTime = true;

    Coroutine _cr;
    Vector3 _baseScale; // base scale "thật" (không bị tích lũy)

    void Awake()
    {
        if (target == null)
            target = transform as RectTransform;

        CacheBaseScale();

        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(Play);
            btn.onClick.AddListener(Play);
        }
    }

    void OnEnable()
    {
        // Nếu bạn có animation/ layout làm thay đổi scale trước khi enable, cache lại
        CacheBaseScale();
        if (target != null) target.localScale = _baseScale;
    }

    void OnDisable()
    {
        if (_cr != null)
        {
            StopCoroutine(_cr);
            _cr = null;
        }

        if (target != null)
            target.localScale = _baseScale;
    }

    void CacheBaseScale()
    {
        if (target != null)
            _baseScale = target.localScale;
    }

    public void Play()
    {
        if (target == null) return;

        // luôn bounce quanh base scale thật
        if (_cr != null)
        {
            StopCoroutine(_cr);
            _cr = null;
        }

        // reset về base trước khi phồng lại (tránh ăn dần)
        target.localScale = _baseScale;

        _cr = StartCoroutine(CoBounce());
    }

    IEnumerator CoBounce()
    {
        Vector3 from = _baseScale;
        Vector3 to = _baseScale * scaleUp;

        float dt;

        // ==== SCALE UP ====
        float t = 0f;
        while (t < 1f)
        {
            dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt / Mathf.Max(0.001f, upTime);
            target.localScale = Vector3.LerpUnclamped(from, to, EaseOut(t));
            yield return null;
        }

        // ==== SCALE DOWN ====
        t = 0f;
        while (t < 1f)
        {
            dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt / Mathf.Max(0.001f, downTime);
            target.localScale = Vector3.LerpUnclamped(to, from, EaseOut(t));
            yield return null;
        }

        target.localScale = from;
        _cr = null;
    }

    float EaseOut(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - Mathf.Pow(1f - x, 3f); // ease out cubic
    }
}