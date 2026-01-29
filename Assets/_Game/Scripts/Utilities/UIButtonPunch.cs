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
    Vector3 _baseScale;

    void Awake()
    {
        if (target == null)
            target = transform as RectTransform;

        _baseScale = target.localScale;

        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(Play);
            btn.onClick.AddListener(Play);
        }
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

    public void Play()
    {
        if (target == null) return;

        if (_cr != null)
            StopCoroutine(_cr);

        _baseScale = target.localScale;
        _cr = StartCoroutine(CoBounce());
    }

    IEnumerator CoBounce()
    {
        Vector3 from = _baseScale;
        Vector3 to = _baseScale * scaleUp;

        // ==== SCALE UP ====
        float t = 0f;
        while (t < 1f)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / Mathf.Max(0.001f, upTime);
            target.localScale = Vector3.LerpUnclamped(from, to, EaseOut(t));
            yield return null;
        }

        // ==== SCALE DOWN (nhún nhẹ) ====
        t = 0f;
        while (t < 1f)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / Mathf.Max(0.001f, downTime);
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
